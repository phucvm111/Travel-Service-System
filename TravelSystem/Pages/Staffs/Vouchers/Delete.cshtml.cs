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

            // ✅ DÙNG CHUNG: chỉ cần check UserId
            Voucher = await _context.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.VoucherId == id && x.UserId == userId.Value);

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
                .FirstOrDefaultAsync(x => x.VoucherId == id && x.UserId == userId.Value);

            if (voucherInDb == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher để thay đổi trạng thái.";
                return RedirectToPage("./Index");
            }

            // ✅ LOGIC CHUẨN CHO CẢ AGENT + STAFF
            if (voucherInDb.Status == 1 || voucherInDb.Status == 2)
            {
                // Đang active → chuyển inactive
                voucherInDb.Status = 0;
            }
            else if (voucherInDb.Status == 0)
            {
                // Inactive → quay lại trạng thái gốc
                // 👉 dựa vào loại user tạo
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId.Value);

                if (user != null)
                {
                    // giả sử:
                    // Role = "Staff" hoặc "Agent"
                    if (user.RoleId == 4)
                        voucherInDb.Status = 1;
                    else
                        voucherInDb.Status = 2;
                }
                else
                {
                    // fallback (an toàn)
                    voucherInDb.Status = 1;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "ĐÃ THAY ĐỔI TRẠNG THÁI VOUCHER THÀNH CÔNG";
            return RedirectToPage("./Index");
        }
    }
}