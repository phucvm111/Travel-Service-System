using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class EntertainmentsDetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public EntertainmentsDetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Entertainment? Entertainment { get; set; }
        public Service? Service { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Entertainment = await _context.Entertainments
                .AsNoTracking()
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.ServiceId == id
                                       && x.Service != null
                                       && x.Service.ServiceType == 3);

            if (Entertainment == null || Entertainment.Service == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ giải trí.";
                return RedirectToPage("/Users/Tours/Search");
            }

            Service = Entertainment.Service;
            return Page();
        }
    }
}