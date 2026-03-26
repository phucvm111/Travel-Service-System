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

            var staff = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StaffId == userId.Value);

            if (staff == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin nhân viên.";
                return RedirectToPage("/Error");
            }

            Voucher = await _context.Vouchers
                .AsNoTracking()
                .Include(x => x.Staff)
                .FirstOrDefaultAsync(x => x.VoucherId == id && x.StaffId == staff.StaffId);

            if (Voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}