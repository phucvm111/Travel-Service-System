using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Entertainments
{
    public class DeleteModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DeleteModel(FinalPrnContext context)
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
                                       && x.Service.ServiceType == 3);

            if (Entertainment == null || Entertainment.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ giải trí.";
                return RedirectToPage("./Index");
            }

            Service = Entertainment.Service;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đại lý.";
                return RedirectToPage("/Error");
            }

            var entertainmentInDb = await _context.Entertainments
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.AgentId == agent.TravelAgentId
                                       && x.Service.ServiceType == 3);

            if (entertainmentInDb == null || entertainmentInDb.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ giải trí để thay đổi trạng thái.";
                return RedirectToPage("./Index");
            }

            entertainmentInDb.Service.Status = entertainmentInDb.Service.Status == 1 ? 0 : 1;
            entertainmentInDb.Service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "ĐÃ THAY ĐỔI TRẠNG THÁI GIẢI TRÍ THÀNH CÔNG";
            return RedirectToPage("./Index");
        }
    }
}