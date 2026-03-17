using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private const int CompletedBookingStatus = 3;

        public IndexModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public int TotalAccounts { get; set; }
        public int TotalAgents { get; set; }
        public int TotalStaffs { get; set; }
        public int TotalTourists { get; set; }
        public int TotalTours { get; set; }
        public double TotalRevenue { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1)
            {
                return RedirectToPage("/Auths/Login");
            }

            TotalAccounts = await _context.Users.CountAsync();

            TotalAgents = await _context.Users.CountAsync(u => u.RoleId == 3);
            TotalTourists = await _context.Users.CountAsync(u => u.RoleId == 2);
            TotalStaffs = await _context.Users.CountAsync(u => u.RoleId == 1);

            TotalTours = await _context.Tours.CountAsync();

            TotalRevenue = await _context.BookDetails
                .Where(b => b.Status == CompletedBookingStatus)
                .SumAsync(b => (double?)b.TotalPrice) ?? 0;

            ChartLabels = new List<string>
            {
                "Đại Lý Du Lịch",
                "Nhân Viên",
                "Khách Du Lịch"
            };

            ChartData = new List<int>
            {
                TotalAgents,
                TotalStaffs,
                TotalTourists
            };

            return Page();
        }
    }
}