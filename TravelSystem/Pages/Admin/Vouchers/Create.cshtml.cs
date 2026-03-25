using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Vouchers
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

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Mã thẻ: 5-25 ký tự, viết hoa
            if (string.IsNullOrWhiteSpace(Voucher.VoucherCode) ||
                Voucher.VoucherCode.Length < 5 ||
                Voucher.VoucherCode.Length > 25 ||
                Voucher.VoucherCode != Voucher.VoucherCode.ToUpper())
            {
                ModelState.AddModelError("Voucher.VoucherCode", "Mã thẻ phải từ 5-25 ký tự và luôn viết hoa");
            }

            // Tên voucher: 5-50 ký tự
            if (string.IsNullOrWhiteSpace(Voucher.VoucherName) ||
                Voucher.VoucherName.Length < 5 ||
                Voucher.VoucherName.Length > 50)
            {
                ModelState.AddModelError("Voucher.VoucherName", "Tên voucher phải nằm trong 5-50 kí tự");
            }

            // Mô tả: tối thiểu 10 ký tự
            if (string.IsNullOrWhiteSpace(Voucher.Description) ||
                Voucher.Description.Length < 10)
            {
                ModelState.AddModelError("Voucher.Description", "Mô tả phải trên 10 kí tự");
            }

            // % giảm giá
            if (Voucher.PercentDiscount < 0 || Voucher.PercentDiscount > 100)
            {
                ModelState.AddModelError("Voucher.PercentDiscount", "Giảm giá không thể âm hoặc lớn hơn 100");
            }

            // Giảm tối đa
            if (Voucher.MaxDiscountAmount < 0 || Voucher.MaxDiscountAmount > 10000000)
            {
                ModelState.AddModelError("Voucher.MaxDiscountAmount", "Giảm tối đa không thể âm và nhỏ hơn 10.000.000");
            }

            // Giá tối thiểu áp dụng
            if (Voucher.MinAmountApply < 0)
            {
                ModelState.AddModelError("Voucher.MinAmountApply", "Giá tiền tối thiểu để áp dụng không thể âm");
            }

            // Ngày bắt đầu
            if (Voucher.StartDate < DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError("Voucher.StartDate", "Ngày bắt đầu không được ở quá khứ");
            }

            // Ngày kết thúc
            if (Voucher.EndDate < Voucher.StartDate)
            {
                ModelState.AddModelError("Voucher.EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            // Số lượng
            if (Voucher.Quantity < 0 || Voucher.Quantity > 100)
            {
                ModelState.AddModelError("Voucher.Quantity", "Số lượng không thể âm và lớn hơn 100");
            }

            // Kiểm tra trùng mã voucher
            bool isDuplicateCode = _context.Vouchers.Any(v => v.VoucherCode == Voucher.VoucherCode);
            if (isDuplicateCode)
            {
                ModelState.AddModelError("Voucher.VoucherCode", "Mã thẻ đã tồn tại");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thêm thất bại";
                return Page();
            }

            try
            {
                _context.Vouchers.Add(Voucher);
                _context.SaveChanges();

                TempData["Success"] = "Thêm voucher thành công.";
                return RedirectToPage("/Admin/Vouchers/Index");
            }
            catch
            {
                TempData["Error"] = "Nhập không khả dụng, vui lòng nhập lại";
                return Page();
            }
        }
    }
}