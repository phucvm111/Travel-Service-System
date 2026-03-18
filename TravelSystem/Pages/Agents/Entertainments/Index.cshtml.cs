using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Entertainments
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public List<EntertainmentListVM> EntertainmentList { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            int travelAgentId = GetCurrentTravelAgentId();

            var query =
                from e in _context.Entertainments
                join s in _context.Services on e.ServiceId equals s.ServiceId
                where s.ServiceType == "ENTERTAINMENT"
                      && s.TravelAgentId == travelAgentId
                select new EntertainmentListVM
                {
                    EntertainmentId = e.EntertainmentId,
                    ServiceId = e.ServiceId,
                    Name = e.Name,
                    Image = e.Image,
                    Type = e.EntertainmentType,
                    OpenTime = e.TimeOpen,
                    CloseTime = e.TimeClose,
                    TicketPrice = e.TicketPrice,
                    AverageRating = e.Rate,
                    Status = e.Status
                };

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(x => x.Name != null && x.Name.Contains(SearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                query = query.Where(x => x.Status == StatusFilter);
            }

            EntertainmentList = await query
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        private int GetCurrentTravelAgentId()
        {
            var claimValue = User.FindFirst("TravelAgentId")?.Value;
            if (int.TryParse(claimValue, out int travelAgentId))
            {
                return travelAgentId;
            }

            return 1;
        }

        public class EntertainmentListVM
        {
            public int EntertainmentId { get; set; }
            public int ServiceId { get; set; }
            public string? Name { get; set; }
            public string? Image { get; set; }
            public string? Type { get; set; }
            public double? AverageRating { get; set; }
            public string? Status { get; set; }
            public TimeOnly? OpenTime { get; set; }
            public TimeOnly? CloseTime { get; set; }
            public double? TicketPrice { get; set; }
        }
    }
}