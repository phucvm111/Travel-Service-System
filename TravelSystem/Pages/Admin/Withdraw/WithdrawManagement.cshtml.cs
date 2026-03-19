using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Withdraw
{
    // ??m b?o tên Class là WithdrawManagementModel
    public class WithdrawManagementModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public WithdrawManagementModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public List<WithdrawRequest> WithdrawList { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } // Tên bi?n là Status

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            const int PAGE_SIZE = 10;
            CurrentPage = page < 1 ? 1 : page;

            var query = _context.WithdrawRequests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Keyword))
            {
                query = query.Where(w => w.Email.Contains(Keyword.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(Status))
            {
                query = query.Where(w => w.Status == Status);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PAGE_SIZE);

            WithdrawList = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((CurrentPage - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            return Page();
        }
    }
}