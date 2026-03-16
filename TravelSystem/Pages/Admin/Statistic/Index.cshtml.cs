using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TravelSystem.Pages.Admin.Statistic
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private const int CompletedStatus = 3;
        private const int PageSize = 10;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public class RevenueRowVm
        {
            public int BookId { get; set; }
            public long? BookCode { get; set; }
            public string TourName { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public double TotalPrice { get; set; }
            public double TaxAmount { get; set; }
            public DateOnly? BookDate { get; set; }
            public string StatusText { get; set; } = "";
        }

        public List<RevenueRowVm> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }
        public int TotalCompletedOrders { get; set; }
        public double TotalRevenue { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
                return RedirectToPage("/Auths/Login");

            var query = _context.BookDetails
                .Include(b => b.Tour)
                .Include(b => b.User)
                .Where(b => b.Status == CompletedStatus)
                .AsQueryable();

            var today = DateOnly.FromDateTime(DateTime.Today);

            switch ((DateFilter ?? "all").ToLower())
            {
                case "today":
                    query = query.Where(b => b.BookDate == today);
                    break;

                case "month":
                    query = query.Where(b => b.BookDate.HasValue
                        && b.BookDate.Value.Month == today.Month
                        && b.BookDate.Value.Year == today.Year);
                    break;

                case "year":
                    query = query.Where(b => b.BookDate.HasValue
                        && b.BookDate.Value.Year == today.Year);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var keyword = SearchTerm.Trim().ToLower();

                query = query.Where(b =>
                    (b.BookCode != null && b.BookCode.ToString()!.Contains(keyword)) ||
                    (((b.FirstName ?? "") + " " + (b.LastName ?? "")).ToLower().Contains(keyword)) ||
                    (b.Tour != null && b.Tour.TourName != null && b.Tour.TourName.ToLower().Contains(keyword)));
            }

            var activeVat = await _context.Vats
                .Where(v => v.Status == 1)
                .OrderByDescending(v => v.StartDate)
                .FirstOrDefaultAsync();

            double vatRate = activeVat?.VatRate ?? 0;

            TotalCompletedOrders = await query.CountAsync();
            TotalRevenue = await query.SumAsync(b => (double?)b.TotalPrice) ?? 0;

            TotalPages = (int)Math.Ceiling(TotalCompletedOrders / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Orders = await query
                .OrderByDescending(b => b.BookDate)
                .ThenByDescending(b => b.BookId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(b => new RevenueRowVm
                {
                    BookId = b.BookId,
                    BookCode = b.BookCode,
                    TourName = b.Tour != null ? (b.Tour.TourName ?? "") : "",
                    CustomerName = ((b.FirstName ?? "") + " " + (b.LastName ?? "")).Trim(),
                    TotalPrice = b.TotalPrice ?? 0,
                    TaxAmount = (b.TotalPrice ?? 0) * vatRate / 100.0,
                    BookDate = b.BookDate,
                    StatusText = b.Status == CompletedStatus ? "Đã hoàn thành" : "Khác"
                })
                .ToListAsync();

            return Page();
        }
    }
}
