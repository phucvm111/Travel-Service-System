using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TravelSystem.Models;

namespace TravelSystem.Pages.Admin.Statistic
{
    public class IndexModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public IndexModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PaymentMethodId { get; set; }

        public List<RevenueRowViewModel> RevenueRows { get; set; } = new();
        public List<MonthlyRevenueViewModel> MonthlyRevenue { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();

        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Lấy danh mục Payment Methods
            PaymentMethods = await _context.PaymentMethods
                .AsNoTracking()
                .OrderBy(x => x.MethodName)
                .ToListAsync();

            // 2. Query cơ sở
            var bookingQuery = _context.Bookings
                .AsNoTracking()
                .Where(b => b.Status == 1);

            // 3. Lọc theo ngày (Chuyển DateTime từ Filter sang DateOnly để so sánh với DB)
            if (FromDate.HasValue)
            {
                var fromDateOnly = DateOnly.FromDateTime(FromDate.Value);
                bookingQuery = bookingQuery.Where(b => b.TourDeparture != null && b.TourDeparture.StartDate >= fromDateOnly);
            }

            if (ToDate.HasValue)
            {
                var toDateOnly = DateOnly.FromDateTime(ToDate.Value);
                bookingQuery = bookingQuery.Where(b => b.TourDeparture != null && b.TourDeparture.StartDate <= toDateOnly);
            }

            if (PaymentMethodId.HasValue)
            {
                bookingQuery = bookingQuery.Where(b => b.PaymentMethodId == PaymentMethodId.Value);
            }

            // 4. Lấy dữ liệu về bộ nhớ (Sử dụng Anonymous type để tránh lỗi Translation)
            var rawData = await bookingQuery
                .OrderByDescending(b => b.BookDate)
                .ThenByDescending(b => b.BookId)
                .Select(b => new
                {
                    b.BookId,
                    b.BookCode,
                    b.BookDate, // Đây là DateOnly?
                    b.FirstName,
                    b.LastName,
                    b.Phone,
                    TourName = (b.TourDeparture != null && b.TourDeparture.Tour != null) ? b.TourDeparture.Tour.TourName : "",
                    DepartureDateOnly = b.TourDeparture != null ? b.TourDeparture.StartDate : null,
                    PaymentMethodName = b.PaymentMethod != null ? b.PaymentMethod.MethodName : "",
                    TotalPrice = b.TotalPrice ?? 0,
                    b.Status
                })
                .ToListAsync();

            // 5. Map sang ViewModel và xử lý kiểu dữ liệu
            RevenueRows = rawData.Select(b => new RevenueRowViewModel
            {
                BookId = b.BookId,
                BookCode = b.BookCode,
                // Chuyển DateOnly? sang DateTime? để hiển thị ở View dễ hơn
                BookDate = b.BookDate?.ToDateTime(TimeOnly.MinValue),
                CustomerName = $"{(b.FirstName ?? "").Trim()} {(b.LastName ?? "").Trim()}".Trim(),
                Phone = b.Phone,
                TourName = b.TourName,
                DepartureDate = b.DepartureDateOnly?.ToDateTime(TimeOnly.MinValue),
                PaymentMethodName = b.PaymentMethodName,
                TotalPrice = Convert.ToDecimal(b.TotalPrice),
                Status = b.Status
            }).ToList();

            // 6. Tính toán thống kê (Dùng DateOnly để so sánh chính xác)
            var today = DateOnly.FromDateTime(DateTime.Today);

            TotalBookings = RevenueRows.Count;
            TotalRevenue = RevenueRows.Sum(x => x.TotalPrice);
            AverageRevenue = TotalBookings > 0 ? TotalRevenue / TotalBookings : 0;

            // TodayRevenue: So sánh DateOnly với DateOnly
            TodayRevenue = rawData
                .Where(x => x.BookDate.HasValue && x.BookDate.Value == today)
                .Sum(x => (decimal)x.TotalPrice);

            // ThisMonthRevenue: Truy cập .Month và .Year trực tiếp từ DateOnly
            ThisMonthRevenue = rawData
                .Where(x => x.BookDate.HasValue
                    && x.BookDate.Value.Month == today.Month
                    && x.BookDate.Value.Year == today.Year)
                .Sum(x => (decimal)x.TotalPrice);

            // Monthly Revenue Chart
            MonthlyRevenue = rawData
                .Where(x => x.BookDate.HasValue)
                .GroupBy(x => new { x.BookDate!.Value.Year, x.BookDate.Value.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyRevenueViewModel
                {
                    Label = $"{g.Key.Month:00}/{g.Key.Year}",
                    Revenue = g.Sum(x => (decimal)x.TotalPrice)
                })
                .ToList();

            return Page();
        }

        public string FormatCurrency(decimal value)
        {
            return string.Format(new CultureInfo("vi-VN"), "{0:N0} đ", value);
        }

        public class RevenueRowViewModel
        {
            public int BookId { get; set; }
            public long? BookCode { get; set; }
            public DateTime? BookDate { get; set; } // Để DateTime để thuận tiện cho Format trong HTML
            public string CustomerName { get; set; } = "";
            public string? Phone { get; set; }
            public string TourName { get; set; } = "";
            public DateTime? DepartureDate { get; set; }
            public string PaymentMethodName { get; set; } = "";
            public decimal TotalPrice { get; set; }
            public int? Status { get; set; }
        }

        public class MonthlyRevenueViewModel
        {
            public string Label { get; set; } = "";
            public decimal Revenue { get; set; }
        }
    }
}