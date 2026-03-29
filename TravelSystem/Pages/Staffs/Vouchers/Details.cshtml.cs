using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.Vouchers
{
    public class DetailsModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DetailsModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Voucher? Voucher { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            Voucher = await _context.Vouchers
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.VoucherId == id
                                      && x.UserId == userId.Value);

            if (Voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}