using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.CancelBooking
{
    public class RequestAgentCancelDetailModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public RequestAgentCancelDetailModel(FinalPrnContext context) => _context = context;

        public RequestCancel RequestItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4) return RedirectToPage("/Auths/Login");

            RequestItem = await _context.RequestCancels
                .Include(r => r.Book).ThenInclude(b => b.TourDeparture).ThenInclude(td => td.Tour).ThenInclude(t => t.TravelAgent)
                .Include(r => r.Book.User)
                .FirstOrDefaultAsync(m => m.RequestCancelId == id);

            if (RequestItem == null) return NotFound();
            return Page();
        }
    }
}