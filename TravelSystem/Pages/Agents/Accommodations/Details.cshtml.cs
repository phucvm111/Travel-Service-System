using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
{
    public class DetailsModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public DetailsModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public Accommodation Accommodation { get; set; } = default!;
        public List<Room> Rooms { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null) return RedirectToPage("/Error");

            Accommodation = await _context.Accommodations
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.AccommodationId == id
                    && a.Service != null
                    && a.Service.TravelAgentId == agent.TravelAgentId
                    && a.Service.ServiceType == "ACCOMMODATION");

            if (Accommodation == null) return NotFound();

            Rooms = await _context.Rooms
                .Where(r => r.AccommodationId == id)
                .OrderBy(r => r.RoomId)
                .ToListAsync();

            return Page();
        }
    }
}