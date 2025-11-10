using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MovieCatalog.Web.Utils;
using MovieCatalogApi.Entities;
using MovieCatalogApi.Services;

namespace MovieCatalog.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IMovieCatalogDataService _dataService;

        public IndexModel(ILogger<IndexModel> logger, IMovieCatalogDataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public TitleSort TitleSort { get; set; } = TitleSort.ReleaseYear;
        [BindProperty(SupportsGet = true)] public bool SortDescending { get; set; } = true;
        [BindProperty(SupportsGet = true)] public TitleFilter Filter { get; set; } = TitleFilter.Empty;

        public Dictionary<Genre, int> GenresWithCounts { get; private set; } = new();
        public IReadOnlyCollection<Title> LatestMovies => Titles.Results;
        public PagedResult<Title> Titles { get; private set; } = PagedResult<Title>.Empty;
        public async Task<IActionResult> OnGet()
        {
            if (!Request.Query.ContainsKey(nameof(PageSize)) ||
                !Request.Query.ContainsKey(nameof(PageNumber)) ||
                !Request.Query.ContainsKey(nameof(TitleSort)) ||
                !Request.Query.ContainsKey(nameof(SortDescending)))
            {
                return RedirectToPage("/Index", new
                {
                    PageSize = 20,
                    PageNumber = 1,
                    TitleSort = TitleSort.ReleaseYear,
                    SortDescending = true
                });
            }
            GenresWithCounts = await _dataService.GetGenresWithTitleCountsAsync();

            Titles = await _dataService.GetTitlesAsync(
                pageSize: PageSize,
                page: PageNumber,
                filter: Filter,
                titleSort: TitleSort,
                sortDescending: SortDescending
            );

            return Page();
        }

        public IReadOnlyList<SelectListItem> PageSizeOptions => new[]
        {
            new SelectListItem("10 items/page", "10", PageSize == 10),
            new SelectListItem("20 items/page", "20", PageSize == 20),
            new SelectListItem("30 items/page", "30", PageSize == 30),
            new SelectListItem("60 items/page", "60", PageSize == 60),
            new SelectListItem("120 items/page", "120", PageSize == 120)
        };

        public IReadOnlyList<SelectListItem> TitleSortOptions => Enum
            .GetValues(typeof(TitleSort))
            .Cast<TitleSort>()
            .Select(v => new SelectListItem(v.ToString(), v.ToString(), v == TitleSort))
            .ToList();

        public IReadOnlyList<SelectListItem> SortDirectionOptions => new[]
        {
            new SelectListItem("Ascending", "false", SortDescending == false),
            new SelectListItem("Descending", "true", SortDescending == true)
        };

        public IReadOnlyList<int> PageNumberOptions => new[]
            {
                1, 2, 3,
                PageNumber - 1, PageNumber, PageNumber + 1,
                Titles.LastPageNumber - 1, Titles.LastPageNumber, Titles.LastPageNumber + 1
            }
            .Where(i => i > 0 && i <= Titles.LastPageNumber + 1)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();
    }
}
