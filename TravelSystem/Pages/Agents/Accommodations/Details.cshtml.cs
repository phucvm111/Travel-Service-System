using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
{
    public class DetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Accommodation Accommodation { get; set; } = default!;
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null) return RedirectToPage("/Error");

            Accommodation = await _context.Accommodations
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.ServiceId == id
                    && a.Service != null
                    && a.Service.AgentId == agent.TravelAgentId
                    && a.Service.ServiceType == 1);

            if (Accommodation == null) return NotFound();

            return Page();
        }
    }
}