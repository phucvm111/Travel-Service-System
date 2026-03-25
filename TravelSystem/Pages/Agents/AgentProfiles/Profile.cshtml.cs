using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.AgentProfiles
{
    public class ProfileModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public ProfileModel(FinalPrnContext context) => _context = context;

        public TravelAgent? Agent { get; set; }
        public User? CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            CurrentUser = await _context.Users.FindAsync(userId.Value);
            Agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (Agent == null) return NotFound();
            return Page();
        }
    }
}