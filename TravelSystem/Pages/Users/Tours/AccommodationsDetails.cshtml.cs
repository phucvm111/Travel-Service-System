using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class AccommodationsDetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public AccommodationsDetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Accommodation? Accommodation { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Accommodation = await _context.Accommodations
                .AsNoTracking()
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                    && x.Service != null
                    && x.Service.ServiceType == 1);

            if (Accommodation == null || Accommodation.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách sạn.";
                return RedirectToPage("/Users/Tours/Search");
            }

            return Page();
        }
    }
}