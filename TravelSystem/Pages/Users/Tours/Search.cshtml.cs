using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class SearchModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public SearchModel(FinalPrnContext context)
        {
            _context = context;
        }

        public IList<Tour> Tours { get; set; } = new List<Tour>();
        public List<string> StartPlaces { get; set; } = new();
        public List<string> EndPlaces { get; set; } = new();
        public List<TravelAgent> TravelAgents { get; set; } = new(); // Danh sách đại lý cho dropdown
        public int TotalTours { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        private int PageSize = 5;

        // BIND PROPERTIES
        [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
        [BindProperty(SupportsGet = true)] public string? Budget { get; set; }
        [BindProperty(SupportsGet = true)] public string? Departure { get; set; }
        [BindProperty(SupportsGet = true)] public string? Destination { get; set; }
        [BindProperty(SupportsGet = true)] public DateOnly? DepartureDate { get; set; }
        [BindProperty(SupportsGet = true)] public int? AgentId { get; set; } // Thuộc tính lọc theo Đại lý
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            var query = _context.Tours
                .Include(t => t.TourDepartures)
                .Include(t => t.TravelAgent) // Quan trọng: Include để lấy tên đại lý hiển thị
                .AsQueryable();

            // 1. Lọc theo Tên Tour
            if (!string.IsNullOrEmpty(Keyword))
                query = query.Where(t => t.TourName.Contains(Keyword));

            // 2. Lọc theo Điểm đến
            if (!string.IsNullOrEmpty(Destination))
                query = query.Where(t => t.EndPlace.Contains(Destination));

            // 3. Lọc theo Điểm khởi hành
            if (!string.IsNullOrEmpty(Departure))
                query = query.Where(t => t.StartPlace == Departure);

            // 4. Lọc theo Ngân sách
            if (!string.IsNullOrEmpty(Budget))
            {
                switch (Budget)
                {
                    case "under5":
                        query = query.Where(t => t.TourDepartures.Any(d => d.AdultPrice < 5000000));
                        break;
                    case "5-10":
                        query = query.Where(t => t.TourDepartures.Any(d => d.AdultPrice >= 5000000 && d.AdultPrice <= 10000000));
                        break;
                    case "10-20":
                        query = query.Where(t => t.TourDepartures.Any(d => d.AdultPrice >= 10000000 && d.AdultPrice <= 20000000));
                        break;
                    case "over20":
                        query = query.Where(t => t.TourDepartures.Any(d => d.AdultPrice > 20000000));
                        break;
                }
            }

            // 5. Lọc theo Đại lý
            if (AgentId.HasValue)
                query = query.Where(t => t.TravelAgentId == AgentId.Value);

            // 6. Lọc theo Ngày khởi hành
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (DepartureDate.HasValue)
                query = query.Where(t => t.TourDepartures.Any(d => d.StartDate >= DepartureDate.Value));
            else
                query = query.Where(t => t.TourDepartures.Any(d => d.StartDate >= today));

            // PHÂN TRANG
            TotalTours = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalTours / PageSize);
            CurrentPage = PageNumber;

            Tours = await query
                .OrderBy(t => t.TourName)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Dữ liệu cho Dropdowns
            StartPlaces = await _context.Tours.Select(t => t.StartPlace).Distinct().ToListAsync();
            EndPlaces = await _context.Tours.Select(t => t.EndPlace).Distinct().ToListAsync();
            TravelAgents = await _context.TravelAgents.OrderBy(a => a.TravelAgentName).ToListAsync();
        }
    }
}