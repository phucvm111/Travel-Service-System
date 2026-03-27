using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

            // ===== VALIDATE =====

            // VAT RATE
            if (Vat.VatRate == null)
            {
                ModelState.AddModelError("Vat.VatRate", "Vui lòng nhập tỷ lệ VAT.");
            }
            else if (Vat.VatRate < 0 || Vat.VatRate > 100)
            {
                ModelState.AddModelError("Vat.VatRate", "Tỷ lệ VAT phải từ 0 đến 100.");
            }

            // DESCRIPTION
            if (string.IsNullOrWhiteSpace(Vat.Description))
            {
                ModelState.AddModelError("Vat.Description", "Mô tả không được để trống.");
            }
            else if (Vat.Description.Length > 255)
            {
                ModelState.AddModelError("Vat.Description", "Mô tả tối đa 255 ký tự.");
            }

            // DATE
            if (!Vat.StartDate.HasValue)
            {
                ModelState.AddModelError("Vat.StartDate", "Chọn ngày bắt đầu.");
            }

            if (!Vat.EndDate.HasValue)
            {
                ModelState.AddModelError("Vat.EndDate", "Chọn ngày kết thúc.");
            }

            if (Vat.StartDate.HasValue && Vat.EndDate.HasValue)
            {
                if (Vat.EndDate < Vat.StartDate)
                {
                    ModelState.AddModelError("Vat.EndDate", "Ngày kết thúc phải >= ngày bắt đầu.");
                }
            }

            // STATUS
            if (Vat.Status != 0 && Vat.Status != 1)
            {
                ModelState.AddModelError("Vat.Status", "Chọn trạng thái hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ===== CHECK OVERLAP =====
            bool isOverlap = await _context.Vats.AnyAsync(v =>
                v.StartDate <= Vat.EndDate &&
                v.EndDate >= Vat.StartDate
            );

            if (isOverlap)
            {
                ModelState.AddModelError("", "Khoảng thời gian VAT bị trùng với VAT khác.");
                return Page();
            }

            // ===== AUTO STATUS LOGIC =====
            var now = DateOnly.FromDateTime(DateTime.Now);

            if (Vat.StartDate <= now && Vat.EndDate >= now)
            {
                // đang trong thời gian hiệu lực
                if (Vat.Status == 1)
                {
                    // tắt tất cả VAT đang active
                    var activeVats = await _context.Vats
                        .Where(v => v.Status == 1)
                        .ToListAsync();

                    foreach (var item in activeVats)
                    {
                        item.Status = 0;
                        item.UpdateDate = DateTime.Now;
                    }
                }
            }
            else
            {
                // chưa tới hoặc đã hết → không cho active
                Vat.Status = 0;
            }

            // ===== SAVE =====
            Vat.CreateDate = DateTime.Now;
            Vat.UpdateDate = DateTime.Now;

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