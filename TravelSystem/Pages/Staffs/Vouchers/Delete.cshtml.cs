using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.Vouchers
{
    public class DeleteModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DeleteModel(FinalPrnContext context)
        {
            _context = context;
        }

        public Voucher? Voucher { get; set; }

        [BindProperty]
        public string? ChangeReason { get; set; }

        
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            Voucher = await _context.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.VoucherId == id
                                      && x.UserId == userId.Value);

            if (Voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                return RedirectToPage("./Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var voucherInDb = await _context.Vouchers
                .FirstOrDefaultAsync(x => x.VoucherId == id
                                      && x.UserId == userId.Value);

            if (voucherInDb == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher để thay đổi trạng thái.";
                return RedirectToPage("./Index");
            }

            voucherInDb.Status = voucherInDb.Status == 1 ? 0 : 1;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "ĐÃ THAY ĐỔI TRẠNG THÁI VOUCHER THÀNH CÔNG";
            return RedirectToPage("./Index");
        }
    }
}