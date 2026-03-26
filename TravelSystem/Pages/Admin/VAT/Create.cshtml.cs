using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.VAT
{
    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public CreateModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Vat Vat { get; set; } = new Vat();

        public IActionResult OnGet()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToPage("/Auths/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToPage("/Auths/Login");
            }

            if (Vat.VatRate < 0 || Vat.VatRate > 100)
            {
                ModelState.AddModelError("Vat.VatRate", "Tỷ lệ VAT phải trong khoảng từ 0 đến 100.");
            }

            if (Vat.StartDate.HasValue && Vat.EndDate.HasValue && Vat.EndDate < Vat.StartDate)
            {
                ModelState.AddModelError("Vat.EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Vat.CreateDate = DateTime.Now;
            Vat.UpdateDate = DateTime.Now;

            // bảng VAT mới có thêm userID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                Vat.UserId = userId.Value;
            }

            _context.Vats.Add(Vat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm VAT thành công.";
            return RedirectToPage("./Index");
        }
    }
}