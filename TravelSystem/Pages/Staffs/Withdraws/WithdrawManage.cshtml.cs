using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.Withdraws
{
    public class WithdrawManagementModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public WithdrawManagementModel(FinalPrnContext context) => _context = context;

        public List<FundRequest> WithdrawList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // Kiểm tra quyền Staff (RoleId = 4)
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4) return RedirectToPage("/Auths/Login");

            CurrentPage = p;
            int pageSize = 10;

            // Chỉ lấy các yêu cầu rút tiền (Type = WITHDRAW)
            var query = _context.FundRequests
                .Include(f => f.CreateByNavigation) // Để lấy Email người tạo
                .Include(f => f.ApproveByNavigation)
                .Where(f => f.Type == "WITHDRAW")
                .AsQueryable();

            // 1. Lọc theo Email người yêu cầu
            if (!string.IsNullOrEmpty(Keyword))
            {
                query = query.Where(f => f.CreateByNavigation.Gmail.Contains(Keyword));
            }

            // 2. Lọc theo Trạng thái
            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(f => f.Status == Status);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            WithdrawList = await query
                .OrderByDescending(f => f.RequestDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

    }
}