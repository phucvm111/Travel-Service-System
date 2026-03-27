using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class RestaurantsDetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public RestaurantsDetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Restaurant? Restaurant { get; set; }
        public Service? Service { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Restaurant = await _context.Restaurants
                .AsNoTracking()
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.ServiceType == 2);

            if (Restaurant == null || Restaurant.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà hàng.";
                return RedirectToPage("/Users/Tours/Search");
            }

            Service = Restaurant.Service;
            return Page();
        }
    }
}