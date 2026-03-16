using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class SearchModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public SearchModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public IList<Tour> Tours { get; set; } = new List<Tour>();

        public List<string> StartPlaces { get; set; } = new();

        public List<string> EndPlaces { get; set; } = new();

        public int TotalTours { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        private int PageSize = 5;

        [BindProperty(SupportsGet = true)]
        public string? Budget { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Departure { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Destination { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? DepartureDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            var query = _context.Tours.AsQueryable();


            // FILTER BUDGET
            if (!string.IsNullOrEmpty(Budget))
            {
                switch (Budget)
                {
                    case "under5":
                        query = query.Where(t => t.AdultPrice < 5000000);
                        break;

                    case "5-10":
                        query = query.Where(t => t.AdultPrice >= 5000000 && t.AdultPrice <= 10000000);
                        break;

                    case "10-20":
                        query = query.Where(t => t.AdultPrice >= 10000000 && t.AdultPrice <= 20000000);
                        break;

                    case "over20":
                        query = query.Where(t => t.AdultPrice > 20000000);
                        break;
                }
            }


            // FILTER START PLACE
            if (!string.IsNullOrEmpty(Departure))
            {
                query = query.Where(t => t.StartPlace == Departure);
            }


            // FILTER DESTINATION
            if (!string.IsNullOrEmpty(Destination))
            {
                query = query.Where(t => t.EndPlace.Contains(Destination));
            }


            // FILTER DATE
            if (DepartureDate.HasValue)
            {
                query = query.Where(t => t.StartDay >= DepartureDate);
            }


            TotalTours = await query.CountAsync();


            TotalPages = (int)Math.Ceiling((double)TotalTours / PageSize);

            CurrentPage = PageNumber;


            Tours = await query
                .OrderBy(t => t.StartDay)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();


            StartPlaces = await _context.Tours
                .Select(t => t.StartPlace)
                .Distinct()
                .ToListAsync();


            EndPlaces = await _context.Tours
                .Select(t => t.EndPlace)
                .Distinct()
                .ToListAsync();
        }
    }
}