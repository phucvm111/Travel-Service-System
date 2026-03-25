using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.VAT
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        public IList<Vat> VatList { get; set; } = new List<Vat>();

        [BindProperty(SupportsGet = true)]
        public int? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }
        public int TotalItems { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1) return RedirectToPage("/Auths/Login");

            if (PageSize <= 0) PageSize = 10;

            var query = _context.Vats.AsQueryable();

            if (StatusFilter.HasValue)
            {
                query = query.Where(v => v.Status == StatusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var keyword = SearchTerm.Trim().ToLower();
                query = query.Where(v =>
                    (v.Description != null && v.Description.ToLower().Contains(keyword)) ||
                    v.VatRate.ToString().Contains(keyword));
            }

            TotalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            if (TotalPages == 0) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            VatList = await query
                .OrderByDescending(v => v.VatId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id, int pageNumber, int pageSize, int? statusFilter, string? searchTerm)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1) return RedirectToPage("/Auths/Login");

            var vat = await _context.Vats.FirstOrDefaultAsync(v => v.VatId == id);
            if (vat == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy VAT.";
                return RedirectToPage("./Index", new { PageNumber = pageNumber, PageSize = pageSize, StatusFilter = statusFilter, SearchTerm = searchTerm });
            }

            vat.Status = vat.Status == 1 ? 0 : 1;
            vat.UpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thay đổi trạng thái VAT thành công.";
            return RedirectToPage("./Index", new { PageNumber = pageNumber, PageSize = pageSize, StatusFilter = statusFilter, SearchTerm = searchTerm });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int pageNumber, int pageSize, int? statusFilter, string? searchTerm)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1) return RedirectToPage("/Auths/Login");

            var vat = await _context.Vats.FirstOrDefaultAsync(v => v.VatId == id);
            if (vat == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy VAT.";
                return RedirectToPage("./Index", new { PageNumber = pageNumber, PageSize = pageSize, StatusFilter = statusFilter, SearchTerm = searchTerm });
            }

            _context.Vats.Remove(vat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa VAT thành công.";
            return RedirectToPage("./Index", new { PageNumber = pageNumber, PageSize = pageSize, StatusFilter = statusFilter, SearchTerm = searchTerm });
        }
    }
}