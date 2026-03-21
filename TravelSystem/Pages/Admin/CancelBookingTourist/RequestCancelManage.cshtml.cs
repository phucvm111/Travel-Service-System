using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Requests
{
    public class RequestCancelManageModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public RequestCancelManageModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public List<CancelRequestViewModel> ListRequest { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            // Kiểm tra quyền Admin/Staff từ Session
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 1) return RedirectToPage("/Auths/Login");

            const int pageSize = 10;
            CurrentPage = page;

            // 1. Truy vấn dữ liệu nạp kèm các bảng liên quan
            var query = _context.RequestCancels
                .Include(r => r.Book)
                    .ThenInclude(b => b.Tour)
                .AsQueryable();

            // 2. Lọc theo Keyword (Email)
            if (!string.IsNullOrEmpty(Keyword))
            {
                query = query.Where(r => r.Book.Gmail.Contains(Keyword.Trim()));
            }

            // 3. Lọc theo Status
            if (!string.IsNullOrEmpty(Status))
            {
                query = query.Where(r => r.Status == Status);
            }

            // 4. Phân trang
            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var rawList = await query
                .OrderByDescending(r => r.RequestDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 5. Chuyển đổi sang ViewModel và tính toán tiền hoàn trả
            ListRequest = rawList.Select(r => new CancelRequestViewModel
            {
                Request = r,
                RefundAmount = CalculateRefund(r)
            }).ToList();

            return Page();
        }

        private double CalculateRefund(RequestCancel r)
        {
            if (r.Book == null || r.Book.Tour == null) return 0;

            var requestDate = r.RequestDate ?? DateOnly.FromDateTime(DateTime.Now);
            var bookingDate = r.Book.BookDate ?? requestDate;
            var startDate = r.Book.Tour.StartDay ?? requestDate;
            double totalPrice = r.Book.TotalPrice ?? 0;

            // Tính toán logic hoàn tiền
            TimeSpan diffToStart = startDate.ToDateTime(TimeOnly.MinValue) - requestDate.ToDateTime(TimeOnly.MinValue);
            int daysToStart = diffToStart.Days;

            // Giả định hủy trong 24h đầu (Dựa trên RequestDate và BookingDate)
            if (requestDate == bookingDate) return totalPrice;

            if (daysToStart >= 7) return totalPrice * 0.8;
            if (daysToStart >= 3) return totalPrice * 0.5;

            return 0;
        }
    }

    // Helper class để chứa dữ liệu hiển thị
    public class CancelRequestViewModel
    {
        public RequestCancel Request { get; set; }
        public double RefundAmount { get; set; }
    }
}