using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Restaurants
{
    public class DeleteModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DeleteModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Restaurant? Restaurant { get; set; }
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

            Restaurant = await _context.Restaurants
                .AsNoTracking()
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.AgentId == agent.TravelAgentId
                                       && x.Service.ServiceType == 2);

            if (Restaurant == null || Restaurant.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà hàng.";
                return RedirectToPage("./Index");
            }

            Service = Restaurant.Service;
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

            var restaurantInDb = await _context.Restaurants
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.AgentId == agent.TravelAgentId
                                       && x.Service.ServiceType == 2);

            if (restaurantInDb == null || restaurantInDb.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà hàng để thay đổi trạng thái.";
                return RedirectToPage("./Index");
            }

            restaurantInDb.Service.Status = restaurantInDb.Service.Status == 1 ? 0 : 1;
            restaurantInDb.Service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thay đổi trạng thái thành công.";
            return RedirectToPage("./Index");
        }
    }
}