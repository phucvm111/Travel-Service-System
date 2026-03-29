using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.ManageTransaction
{
    public class HistoryPaymentWalletSystemModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public HistoryPaymentWalletSystemModel(FinalPrnContext context) => _context = context;

        public List<TransactionHistory> TransactionList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // Kiểm tra quyền Staff
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId == null || roleId != 1) return RedirectToPage("/Auths/Login");

            CurrentPage = p;
            int pageSize = 10;
            int systemUserId = 1; // ID ví Admin/Hệ thống

            var query = _context.TransactionHistories
                .Where(t => t.UserId == systemUserId)
                .AsQueryable();

            // 1. Lọc theo loại giao dịch (TransactionType)
            if (!string.IsNullOrEmpty(Status) && Status != "0")
            {
                query = query.Where(t => t.TransactionType == Status);
            }

            // 2. Lọc theo khoảng ngày
            if (!string.IsNullOrEmpty(FromDate) && DateTime.TryParse(FromDate, out DateTime from))
            {
                query = query.Where(t => t.TransactionDate >= from);
            }

            if (!string.IsNullOrEmpty(ToDate) && DateTime.TryParse(ToDate, out DateTime to))
            {
                var toFullDay = to.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.TransactionDate <= toFullDay);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            TransactionList = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }
    }
}