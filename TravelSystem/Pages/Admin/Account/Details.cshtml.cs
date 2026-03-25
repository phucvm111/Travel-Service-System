using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Account
{
    public class DetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public User User { get; set; } = default!;
        public TravelAgent? TravelAgent { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            User = user;

            if (user.RoleId == 3)
            {
                TravelAgent = await _context.TravelAgents
                    .FirstOrDefaultAsync(t => t.UserId == id);
            }

            return Page();
        }
    }
}
