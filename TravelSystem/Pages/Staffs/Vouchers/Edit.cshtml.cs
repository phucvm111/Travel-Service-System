using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.Vouchers
{
    public class EditModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public EditModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Voucher Voucher { get; set; } = new Voucher();

        // ================= GET =================
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            // ✅ CHỈ LẤY VOUCHER CỦA CHÍNH USER
            var voucher = await _context.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VoucherId == id && v.UserId == userId.Value);

            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                return RedirectToPage("./Index");
            }

            Voucher = voucher;
            return Page();
        }

        // ================= POST =================
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            ModelState.Remove("Voucher.User");
            ModelState.Remove("Voucher.Bookings");

            ValidateVoucher();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Cập nhật thất bại. Nhập không hợp lệ.";
                return Page();
            }

            // ✅ CHECK QUYỀN
            var existingVoucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == Voucher.VoucherId
                                       && v.UserId == userId.Value);

            if (existingVoucher == null)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa voucher này.";
                return RedirectToPage("./Index");
            }

            // ================= CHECK DUPLICATE =================
            var voucherCodeUpper = Voucher.VoucherCode!.Trim().ToUpper();

            var isDuplicateCode = await _context.Vouchers
                .AnyAsync(v => v.VoucherId != Voucher.VoucherId
                            && v.VoucherCode != null
                            && v.VoucherCode.ToUpper() == voucherCodeUpper);

            if (isDuplicateCode)
            {
                ModelState.AddModelError("Voucher.VoucherCode", "Mã voucher đã tồn tại.");
                TempData["ErrorMessage"] = "Cập nhật thất bại.";
                return Page();
            }

            // ================= UPDATE =================
            existingVoucher.VoucherCode = voucherCodeUpper;
            existingVoucher.VoucherName = Voucher.VoucherName?.Trim();
            existingVoucher.Description = Voucher.Description?.Trim();
            existingVoucher.PercentDiscount = Voucher.PercentDiscount;
            existingVoucher.MaxDiscountAmount = Voucher.MaxDiscountAmount;
            existingVoucher.MinDiscountAmount = Voucher.MinDiscountAmount;
            existingVoucher.StartDate = Voucher.StartDate;
            existingVoucher.EndDate = Voucher.EndDate;
            existingVoucher.Quantity = Voucher.Quantity;

            // ❌ KHÔNG CHO SỬA STATUS
            // existingVoucher.Status = Voucher.Status;

            // ❌ KHÔNG ĐỤNG USER
            // existingVoucher.UserId = Voucher.UserId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật voucher thành công.";
            return RedirectToPage("./Index");
        }

        // ================= VALIDATION =================
        private void ValidateVoucher()
        {
            if (string.IsNullOrWhiteSpace(Voucher.VoucherCode))
            {
                ModelState.AddModelError("Voucher.VoucherCode", "Mã voucher không được để trống.");
            }
            else
            {
                Voucher.VoucherCode = Voucher.VoucherCode.Trim().ToUpper();

                if (Voucher.VoucherCode.Length < 5 || Voucher.VoucherCode.Length > 25)
                {
                    ModelState.AddModelError("Voucher.VoucherCode", "Mã thẻ phải từ 5-25 ký tự.");
                }

                if (!Regex.IsMatch(Voucher.VoucherCode, @"^[A-Z0-9]+$"))
                {
                    ModelState.AddModelError("Voucher.VoucherCode", "Chỉ chứa chữ in hoa và số.");
                }
            }

            if (string.IsNullOrWhiteSpace(Voucher.VoucherName))
            {
                ModelState.AddModelError("Voucher.VoucherName", "Tên voucher không được để trống.");
            }
            else
            {
                Voucher.VoucherName = Voucher.VoucherName.Trim();

                if (Voucher.VoucherName.Length < 5 || Voucher.VoucherName.Length > 50)
                {
                    ModelState.AddModelError("Voucher.VoucherName", "Tên phải 5-50 ký tự.");
                }
            }

            if (!Voucher.PercentDiscount.HasValue || Voucher.PercentDiscount < 0 || Voucher.PercentDiscount > 100)
            {
                ModelState.AddModelError("Voucher.PercentDiscount", "Giảm giá phải từ 0-100%.");
            }

            if (!Voucher.MaxDiscountAmount.HasValue || Voucher.MaxDiscountAmount < 0 || Voucher.MaxDiscountAmount >= 10000000)
            {
                ModelState.AddModelError("Voucher.MaxDiscountAmount", "Giảm tối đa không hợp lệ.");
            }

            if (!Voucher.MinDiscountAmount.HasValue || Voucher.MinDiscountAmount < 0)
            {
                ModelState.AddModelError("Voucher.MinDiscountAmount", "Giá trị tối thiểu không hợp lệ.");
            }

            //if (Voucher.MaxDiscountAmount < Voucher.MinDiscountAmount)
            //{
            //    ModelState.AddModelError("Voucher.MaxDiscountAmount", "Max phải ≥ Min.");
            //}

            if (!Voucher.StartDate.HasValue)
            {
                ModelState.AddModelError("Voucher.StartDate", "Không được để trống.");
            }

            if (!Voucher.EndDate.HasValue || Voucher.EndDate <= Voucher.StartDate)
            {
                ModelState.AddModelError("Voucher.EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (!Voucher.Quantity.HasValue || Voucher.Quantity < 0 || Voucher.Quantity > 100)
            {
                ModelState.AddModelError("Voucher.Quantity", "Số lượng 0-100.");
            }
        }
    }
}