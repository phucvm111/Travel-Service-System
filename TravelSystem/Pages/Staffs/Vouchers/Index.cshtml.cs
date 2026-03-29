using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.Vouchers
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        public IList<Voucher> Vouchers { get; set; } = new List<Voucher>();

        [BindProperty(SupportsGet = true)]
        public string? SearchCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return RedirectToPage("/Auths/Login");

            // ❌ bỏ check staff riêng
            // var staff = await _context.Staff...

            var query = _context.Vouchers
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.UserId == userId.Value); // ✅ dùng chung

            if (!string.IsNullOrWhiteSpace(SearchCode))
            {
                var keyword = SearchCode.Trim();
                query = query.Where(x => x.VoucherCode != null && x.VoucherCode.Contains(keyword));
            }

            if (StatusFilter.HasValue)
            {
                if (StatusFilter.Value == 1)
                {
                    // Hoạt động: gồm cả 1 và 2
                    query = query.Where(x => x.Status == 1 || x.Status == 2);
                }
                else if (StatusFilter.Value == 0)
                {
                    // Ngừng hoạt động
                    query = query.Where(x => x.Status == 0);
                }
            }

            var totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            if (TotalPages == 0) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Vouchers = await query
                .OrderByDescending(x => x.VoucherId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
    }
}