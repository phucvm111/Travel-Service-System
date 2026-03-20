using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;


namespace YourProject.Pages.Admin.Vouchers
{
    public class EditModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public EditModel(Prn222PrjContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Voucher Voucher { get; set; } = default!;

        public IActionResult OnGet(int id)
        {
            var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == id);
            if (voucher == null)
            {
                return RedirectToPage("/Admin/Vouchers/Index");
            }

            Voucher = voucher;
            return Page();
        }

        public IActionResult OnPost()
        {
            var voucherInDb = _context.Vouchers.FirstOrDefault(v => v.VoucherId == Voucher.VoucherId);
            if (voucherInDb == null)
            {
                return RedirectToPage("/Admin/Vouchers/Index");
            }

            if (string.IsNullOrWhiteSpace(Voucher.VoucherName) || Voucher.VoucherName.Length < 5 || Voucher.VoucherName.Length > 50)
            {
                ModelState.AddModelError("Voucher.VoucherName", "Tên voucher phải nằm trong 5-50 kí tự");
            }

            if (Voucher.PercentDiscount < 0 || Voucher.PercentDiscount > 100)
            {
                ModelState.AddModelError("Voucher.PercentDiscount", "Giảm giá không thể âm hoặc lớn hơn 100");
            }

            if (Voucher.MaxDiscountAmount < 0 || Voucher.MaxDiscountAmount > 10000000)
            {
                ModelState.AddModelError("Voucher.MaxDiscountAmount", "Giảm tối đa không thể âm và lớn hơn 10.000.000");
            }

            if (Voucher.MinAmountApply < 0)
            {
                ModelState.AddModelError("Voucher.MinAmountApply", "Giá tiền tối thiểu để áp dụng không thể âm");
            }

            if (Voucher.EndDate < Voucher.StartDate)
            {
                ModelState.AddModelError("Voucher.EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (Voucher.Quantity < 0 || Voucher.Quantity > 100)
            {
                ModelState.AddModelError("Voucher.Quantity", "Số lượng không thể âm và lớn hơn 100");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            voucherInDb.VoucherCode = Voucher.VoucherCode;
            voucherInDb.VoucherName = Voucher.VoucherName;
            voucherInDb.Description = Voucher.Description;
            voucherInDb.PercentDiscount = Voucher.PercentDiscount;
            voucherInDb.MaxDiscountAmount = Voucher.MaxDiscountAmount;
            voucherInDb.MinAmountApply = Voucher.MinAmountApply;
            voucherInDb.StartDate = Voucher.StartDate;
            voucherInDb.EndDate = Voucher.EndDate;
            voucherInDb.Quantity = Voucher.Quantity;
            voucherInDb.Status = Voucher.Status;

            _context.SaveChanges();

            return RedirectToPage("/Admin/Vouchers/Index");
        }
    }
}