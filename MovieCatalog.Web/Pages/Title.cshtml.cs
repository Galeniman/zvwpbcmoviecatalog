using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MovieCatalog.Exceptions;
using MovieCatalogApi.Entities;
using MovieCatalogApi.Services;
using System.ComponentModel.DataAnnotations;

namespace MovieCatalog.Web.Pages
{
    public class TitleModel : PageModel
    {
        private readonly IMovieCatalogDataService _dataService;

        public TitleModel(IMovieCatalogDataService dataService)
        {
            _dataService = dataService;
        }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [BindProperty, Required, StringLength(500)]
        [Display(Name = "Primary title")]
        public string? PrimaryTitle { get; set; }

        [BindProperty, Required, StringLength(500)]
        [Display(Name = "Original title")]
        public string? OriginalTitle { get; set; }

        [BindProperty]
        [Display(Name = "Title type")]
        public TitleType TitleType { get; set; }

        [BindProperty, Range(1900, 2100)]
        [Display(Name = "Start/release year")]
        public int? StartYear { get; set; }

        [BindProperty, Range(1900, 2100)]
        [Display(Name = "Year of last season")]
        public int? EndYear { get; set; }

        [BindProperty, Range(1, 9999)]
        [Display(Name = "General runtime in minutes")]
        public int? RuntimeMinutes { get; set; }

        [BindProperty]
        [Display(Name = "Genres")]
        [MaxLength(3, ErrorMessage = "Maximum 3 genres allowed.")]
        public List<int> Genres { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            if (Id == null)
                return Page();

            try
            {
                var title = await _dataService.GetTitleByIdAsync(Id.Value);

                PrimaryTitle = title.PrimaryTitle;
                OriginalTitle = title.OriginalTitle;
                TitleType = title.TitleType;
                StartYear = title.StartYear;
                EndYear = title.EndYear;
                RuntimeMinutes = title.RuntimeMinutes;
                Genres = title.TitleGenres.Select(tg => tg.GenreId).ToList();

                return Page();
            } catch (ObjectNotFoundException)
            {
                return RedirectToPage("/Title", new { Id = (int?)null });
            }
        }

        public async Task<IReadOnlyCollection<SelectListItem>> GetGenreOptionsAsync()
        {
            var allGenres = await _dataService.GetGenresAsync();
            return allGenres.Select(g => new SelectListItem(g.Name, g.Id.ToString(), Genres.Contains(g.Id))).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _dataService.InsertOrUpdateTitleAsync(
                Id,
                PrimaryTitle,
                OriginalTitle,
                TitleType,
                StartYear,
                EndYear,
                RuntimeMinutes,
                Genres.ToArray()
            );

            SuccessMessage = Id == null
                ? "Movie added successfully."
                : "Movie updated successfully.";

            return RedirectToPage("/Title", new { Id = result.Id });
        }

    }
}
