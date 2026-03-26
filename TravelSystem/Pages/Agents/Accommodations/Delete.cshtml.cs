using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Accommodations
{
    public class DeleteModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DeleteModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Accommodation? Accommodation { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(x => x.UserId == userId.Value);
            if (agent == null)
                return RedirectToPage("/Error");

            Accommodation = await _context.Accommodations
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                    && x.Service != null
                    && x.Service.AgentId == agent.TravelAgentId
                    && x.Service.ServiceType == 1);

            if (Accommodation == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(x => x.UserId == userId.Value);
            if (agent == null)
                return RedirectToPage("/Error");

            var item = await _context.Accommodations
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == Accommodation!.ServiceId
                    && x.Service != null
                    && x.Service.AgentId == agent.TravelAgentId
                    && x.Service.ServiceType == 1);

            if (item == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách sạn để xóa.";
                return RedirectToPage("./Index");
            }

            bool usedInTour = await _context.TourServiceDetails.AnyAsync(x => x.ServiceId == item.ServiceId);
            if (usedInTour)
            {
                TempData["ErrorMessage"] = "Không thể xóa vì khách sạn đang được dùng trong tour.";
                return RedirectToPage("./Index");
            }

            try
            {
                _context.Accommodations.Remove(item);
                if (item.Service != null)
                {
                    _context.Services.Remove(item.Service);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa khách sạn thành công.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa khách sạn.";
            }

            return RedirectToPage("./Index");
        }
    }
}