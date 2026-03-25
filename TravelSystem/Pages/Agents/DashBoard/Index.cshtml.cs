using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        public int TotalTours { get; set; }
        public int TotalRestaurants { get; set; }
        public int TotalEntertainments { get; set; }
        public int TotalAccommodations { get; set; }

        public List<RecentBookingVm> RecentBookings { get; set; } = new();

        public class RecentBookingVm
        {
            public string CustomerName { get; set; } = "";
            public string BookDateText { get; set; } = "";
            public double TotalPrice { get; set; }
            public string StatusText { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (userId == null || roleId != 3)
            {
                return RedirectToPage("/Auths/Login");
            }

            var agent = await _context.TravelAgents
                .FirstOrDefaultAsync(a => a.UserId == userId.Value);

            if (agent == null)
            {
                return RedirectToPage("/Error");
            }

            TotalTours = await _context.Tours
                .CountAsync(t => t.TravelAgentId == agent.TravelAgentId);

            var agentServiceIds = await _context.Services
                .Where(s => s.TravelAgentId == agent.TravelAgentId)
                .Select(s => s.ServiceId)
                .ToListAsync();

            TotalRestaurants = await _context.Restaurants
                .CountAsync(r => agentServiceIds.Contains(r.ServiceId));

            TotalEntertainments = await _context.Entertainments
                .CountAsync(e => agentServiceIds.Contains(e.ServiceId));

            TotalAccommodations = await _context.Accommodations
                .CountAsync(a => agentServiceIds.Contains(a.ServiceId));

            RecentBookings = await _context.BookDetails
                .Include(b => b.Tour)
                .Where(b => b.Tour != null && b.Tour.TravelAgentId == agent.TravelAgentId)
                .OrderByDescending(b => b.BookDate)
                .ThenByDescending(b => b.BookId)
                .Take(5)
                .Select(b => new RecentBookingVm
                {
                    CustomerName = ((b.FirstName ?? "") + " " + (b.LastName ?? "")).Trim(),
                    BookDateText = b.BookDate.HasValue
                        ? b.BookDate.Value.ToString("dd-MM-yyyy")
                        : "",
                    TotalPrice = b.TotalPrice ?? 0,
                    StatusText = b.Status == 1 ? "Chờ xác nhận"
                                : b.Status == 2 ? "Đã xác nhận"
                                : b.Status == 3 ? "Đã hoàn thành"
                                : b.Status == 4 ? "Hủy"
                                : "Khác"
                })
                .ToListAsync();

            return Page();
        }
    }
}