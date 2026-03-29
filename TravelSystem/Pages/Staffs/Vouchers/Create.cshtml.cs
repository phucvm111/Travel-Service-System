using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.Vouchers
{
    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public CreateModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Voucher Voucher { get; set; } = new Voucher();

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (roleId == 3)
            {
                Voucher.Status = 1;
            }
            else if (roleId == 4)
            {
                Voucher.Status = 1;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null)
                return RedirectToPage("/Auths/Login");

            ModelState.Remove("Voucher.User");
            ModelState.Remove("Voucher.Bookings");

            if (roleId == 3)
            {
                Voucher.Status = 1;
            }
            else if (roleId == 4)
            {
                Voucher.Status = 1;
            }

            ValidateVoucher();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thêm thất bại. Nhập không khả dụng, vui lòng nhập lại.";
                return Page();
            }

            var voucherCodeUpper = Voucher.VoucherCode!.Trim().ToUpper();

            var isDuplicateCode = await _context.Vouchers
                .AnyAsync(x => x.VoucherCode != null && x.VoucherCode.ToUpper() == voucherCodeUpper);

            if (isDuplicateCode)
            {
                ModelState.AddModelError("Voucher.VoucherCode", "Mã voucher đã tồn tại.");
                TempData["ErrorMessage"] = "Thêm thất bại. Nhập không khả dụng, vui lòng nhập lại.";
                return Page();
            }

            Voucher.VoucherCode = voucherCodeUpper;
            Voucher.VoucherName = Voucher.VoucherName?.Trim();
            Voucher.Description = Voucher.Description?.Trim();
            Voucher.UserId = userId.Value;

            _context.Vouchers.Add(Voucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm voucher thành công.";
            return RedirectToPage("./Index");
        }

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
                    ModelState.AddModelError("Voucher.VoucherCode", "Mã thẻ phải từ 5-25 ký tự và luôn viết hoa.");
                }

                if (!Regex.IsMatch(Voucher.VoucherCode, @"^[A-Z0-9]+$"))
                {
                    ModelState.AddModelError("Voucher.VoucherCode", "Mã voucher chỉ được chứa chữ in hoa và số.");
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
                    ModelState.AddModelError("Voucher.VoucherName", "Tên voucher phải nằm trong 5-50 kí tự.");
                }
            }

            if (!Voucher.PercentDiscount.HasValue)
            {
                ModelState.AddModelError("Voucher.PercentDiscount", "Phần trăm giảm giá không được để trống.");
            }
            else if (Voucher.PercentDiscount < 0 || Voucher.PercentDiscount > 100)
            {
                ModelState.AddModelError("Voucher.PercentDiscount", "Giảm giá không thể âm hoặc lớn hơn 100.");
            }

            if (!Voucher.MaxDiscountAmount.HasValue)
            {
                ModelState.AddModelError("Voucher.MaxDiscountAmount", "Giảm tối đa không được để trống.");
            }
            else if (Voucher.MaxDiscountAmount < 0 || Voucher.MaxDiscountAmount >= 10000000)
            {
                ModelState.AddModelError("Voucher.MaxDiscountAmount", "Giảm giá tối đa không thể âm và nhỏ hơn 10.000.000.");
            }

            if (!Voucher.MinDiscountAmount.HasValue)
            {
                ModelState.AddModelError("Voucher.MinDiscountAmount", "Giá tiền tối thiểu để áp dụng không được để trống.");
            }
            else if (Voucher.MinDiscountAmount < 0)
            {
                ModelState.AddModelError("Voucher.MinDiscountAmount", "Giá tiền tối thiểu để áp dụng không thể âm.");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (!Voucher.StartDate.HasValue)
            {
                ModelState.AddModelError("Voucher.StartDate", "Ngày bắt đầu không được để trống.");
            }
            else if (Voucher.StartDate < today)
            {
                ModelState.AddModelError("Voucher.StartDate", "Ngày bắt đầu không được ở quá khứ.");
            }

            if (!Voucher.EndDate.HasValue)
            {
                ModelState.AddModelError("Voucher.EndDate", "Ngày kết thúc không được để trống.");
            }
            else if (Voucher.StartDate.HasValue && Voucher.EndDate <= Voucher.StartDate)
            {
                ModelState.AddModelError("Voucher.EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (!Voucher.Quantity.HasValue)
            {
                ModelState.AddModelError("Voucher.Quantity", "Số lượng không được để trống.");
            }
            else if (Voucher.Quantity < 0 || Voucher.Quantity > 100)
            {
                ModelState.AddModelError("Voucher.Quantity", "Số lượng không thể âm và lớn hơn 100.");
            }
        }
    }
}
