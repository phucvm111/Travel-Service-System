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

        public Entertainment? Entertainment { get; set; }
        public Service? Service { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đại lý.";
                return RedirectToPage("/Error");
            }

            Entertainment = await _context.Entertainments
                .AsNoTracking()
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.AgentId == agent.TravelAgentId
                                       && x.Service.ServiceType == 2);

            if (Entertainment == null || Entertainment.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ giải trí.";
                return RedirectToPage("./Index");
            }

            Service = Entertainment.Service;
            return Page();
        }
    }
}