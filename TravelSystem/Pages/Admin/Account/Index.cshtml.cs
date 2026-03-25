using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Account
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hubContext;

        public IndexModel(FinalPrnContext context, IHubContext<HubServer> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public IList<User> UserList { get; set; } = default!;

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            UserList = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int userID)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (userEmail == null)
            {
                return RedirectToPage("/Auths/Login");
            }

            var user = await _context.Users.FindAsync(userID);
            if (user != null)
            {
                user.Status = user.Status == 1 ? 0 : 1;
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("loadAll");
            }

            return RedirectToPage();
        }
    }
}
