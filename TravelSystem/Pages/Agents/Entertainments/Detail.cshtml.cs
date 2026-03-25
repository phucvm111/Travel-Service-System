using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Entertainments
{
    public class DetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public EntertainmentDetailsVM EntertainmentDetail { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int travelAgentId = GetCurrentTravelAgentId();

            var entertainment = await _context.Entertainments
                .Include(e => e.Service)
                .FirstOrDefaultAsync(e => e.EntertainmentId == id
                    && e.Service != null
                    && e.Service.TravelAgentId == travelAgentId);

            if (entertainment == null)
            {
                return NotFound();
            }

            EntertainmentDetail = new EntertainmentDetailsVM
            {
                EntertainmentId = entertainment.EntertainmentId,
                ServiceId = entertainment.ServiceId,
                Name = entertainment.Name,
                Image = entertainment.Image,
                Type = entertainment.EntertainmentType,
                City = entertainment.Address,
                Phone = entertainment.Phone,
                OpenTime = entertainment.TimeOpen,
                CloseTime = entertainment.TimeClose,
                TicketPrice = entertainment.TicketPrice,
                Rate = entertainment.Rate,
                Description = entertainment.Description,
                Status = entertainment.Status,
                DayOfWeekOpen = entertainment.DayOfWeekOpen
            };

            return Page();
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

        public class EntertainmentDetailsVM
        {
            public int EntertainmentId { get; set; }
            public int ServiceId { get; set; }
            public string? Name { get; set; }
            public string? Image { get; set; }
            public string? Type { get; set; }
            public string? City { get; set; }
            public string? Phone { get; set; }
            public TimeOnly? OpenTime { get; set; }
            public TimeOnly? CloseTime { get; set; }
            public double? TicketPrice { get; set; }
            public double? Rate { get; set; }
            public string? Description { get; set; }
            public string? Status { get; set; }
            public string? DayOfWeekOpen { get; set; }
        }
    }
}