using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs
{
    public class RequestCancelManageModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public RequestCancelManageModel(FinalPrnContext context) => _context = context;

        public List<RequestCancel> ListRequest { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // Kiểm tra quyền Staff/Admin
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if ( roleId != 4) return RedirectToPage("/Auths/Login");

            CurrentPage = p;
            int pageSize = 10;

            var query = _context.RequestCancels
                .Include(r => r.Book)
                    .ThenInclude(b => b.TourDeparture)
                        .ThenInclude(d => d.Tour)
                        .Include(r => r.Staff).ThenInclude(s => s.StaffNavigation)
                .AsQueryable();

            // 1. Lọc theo Email khách hàng
            if (!string.IsNullOrEmpty(Keyword))
            {
                query = query.Where(r => r.Book.Gmail.Contains(Keyword));
            }

            // 2. Lọc theo Trạng thái (PENDING, FINISHED, REJECTED)
            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(r => r.Status == Status);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ListRequest = await query
                .OrderByDescending(r => r.RequestDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        // Hàm tính tiền hoàn trả giống logic JSP của bạn
        public decimal CalculateRefund(RequestCancel r)
        {
            if (r.Book == null || r.Book.TourDeparture == null) return 0;

            var requestDate = r.RequestDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            var bookDate = r.Book.BookDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            var startDate = r.Book.TourDeparture.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            decimal totalPrice = (decimal)(r.Book.TotalPrice ?? 0);

            var hoursSinceBooking = (requestDate - bookDate).TotalHours;
            var daysToStart = (startDate - requestDate).TotalDays;

            if (hoursSinceBooking <= 24) return totalPrice; // Hoàn 100%
            if (daysToStart >= 7) return totalPrice * 0.8m;   // Hoàn 80%
            if (daysToStart >= 3) return totalPrice * 0.5m;   // Hoàn 50%

            return 0; // Dưới 3 ngày không hoàn tiền
        }
    }
}