using Microsoft.EntityFrameworkCore;
using MovieCatalogApi.Entities;
using System.Linq.Expressions;
using MovieCatalog.Web.Utils;
using MovieCatalog.Exceptions;

namespace MovieCatalogApi.Services;

public class MovieCatalogDataService : IMovieCatalogDataService
{
    private MovieCatalogDbContext ctx;

    public MovieCatalogDataService(MovieCatalogDbContext pctx)
    {
        ctx = pctx;
    }

    public async Task<Dictionary<Genre, int>> GetGenresWithTitleCountsAsync()
    {
        var genres = await ctx.Genres.Include(g => g.TitleGenres)
            .OrderBy(g => g.Name).ToListAsync();
        return genres.ToDictionary(g  => g, g => g.TitleGenres.Select(tg => tg.TitleId).Count());
    }

    public async Task<PagedResult<Title>> GetTitlesAsync(int pageSize, int page, TitleFilter? filter, TitleSort titleSort, bool sortDescending)
    {
        IQueryable<Title> results = ctx.Titles.Include(t => t.TitleGenres).ThenInclude(tg => tg.Genre);

        if (filter != null)
        {
            foreach (var predicate in new (bool check, Expression<Func<Title, bool>> predicate)[]
                {
                        (filter.EndYearMax != null, t => t.EndYear <= filter.EndYearMax),
                        (filter.EndYearMin != null, t => t.EndYear >= filter.EndYearMin),
                        (filter.RuntimeMinutesMax != null, t => t.RuntimeMinutes <= filter.RuntimeMinutesMax),
                        (filter.RuntimeMinutesMin != null, t => t.RuntimeMinutes >= filter.RuntimeMinutesMin),
                        (filter.StartYearMax != null, t => t.StartYear <= filter.StartYearMax),
                        (filter.StartYearMin != null, t => t.StartYear >= filter.StartYearMin),
                        (filter.TitleContains != null, t => t.PrimaryTitle.Contains(filter.TitleContains!) || t.OriginalTitle!.Contains(filter.TitleContains!) ),
                        (filter.Genres?.Any() == true, t => t.TitleGenres!.Any(tg => filter.Genres!.Any(g => tg.Genre.Name == g))),
                        (filter.TitleTypes?.Any() == true, t => filter.TitleTypes!.Contains(t.TitleType))
                }
                .Where(i => i.check)
                .Select(i => i.predicate))
            {
                results = results.Where(predicate);
            }
        }

        Expression<Func<Title, object?>> sorting = titleSort switch
        {
            TitleSort.PrimaryTitle => t => t.PrimaryTitle,
            TitleSort.ReleaseYear => t => t.StartYear,
            TitleSort.Runtime => t => t.RuntimeMinutes,
            _ => throw new ArgumentException(null, nameof(titleSort))
        };

        results = sortDescending ? results.OrderByDescending(sorting) : results.OrderBy(sorting);

        var pageIndex = page <= 0 ? 0 : page - 1;
        return await PagedResult<Title>.ExecuteQueryAsync(results, pageSize, pageIndex);
    }

    public async Task<IEnumerable<Genre>> GetGenresAsync()
    {
       return await ctx.Genres.OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<Title> GetTitleByIdAsync(int id)
    {
       return await ctx.Titles
        .Include(t => t.TitleGenres).ThenInclude(tg => tg.Genre)
        .SingleOrDefaultAsync(t => t.Id == id)
        ?? throw new ObjectNotFoundException<Title>(id);
    }

    public async Task<Title> InsertOrUpdateTitleAsync(int? id, string? primaryTitle, string? originalTitle, TitleType titleType, int? startYear, int? endYear, int? runtimeMinutes, int[]? genres)
    {
        ArgumentNullException.ThrowIfNull(primaryTitle);
        ArgumentNullException.ThrowIfNull(originalTitle);

       Title e;
        if (id == null)
        {
            e = new Title(primaryTitle, originalTitle);
            ctx.Titles.Add(e);
        }
        else
        {
            e=await ctx.Titles.Include(t=>t.TitleGenres).SingleOrDefaultAsync(t => t.Id == id) ?? throw new ObjectNotFoundException<Title>(id);
        }

        e.PrimaryTitle = primaryTitle;
        e.OriginalTitle = originalTitle;
        e.TitleType = titleType;
        e.StartYear = startYear;
        e.EndYear = endYear;
        e.RuntimeMinutes = runtimeMinutes;

        
        if (genres != null)
        {
            foreach (var tg in e.TitleGenres)
            {
                if (!genres.Contains(tg.GenreId))
                    ctx.Remove(tg);
            }
            foreach (var gid in genres.Except(e.TitleGenres.Select(tg => tg.GenreId)))
            {
                    e.TitleGenres.Add(new TitleGenre{GenreId = gid});
            }
        }

        await ctx.SaveChangesAsync();
        return e;
    }

}