using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.CancelBooking
{
    public class ManageRequestAgentCancelModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public ManageRequestAgentCancelModel(FinalPrnContext context) => _context = context;

        // Đổi List từ RequestCancel sang Booking
        public List<Booking> BookingList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Keyword { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4) return RedirectToPage("/Auths/Login");

            CurrentPage = p;
            int pageSize = 10;

            // Truy vấn trực tiếp từ bảng Booking
            var query = _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.Tour)
                .Include(b => b.RequestCancels) // Include thêm để lấy lý do nếu có
                .Where(b => b.Status == 3) // CHỈ LẤY BOOKING CÓ STATUS = 3
                .AsQueryable();

            if (!string.IsNullOrEmpty(Keyword))
            {
                query = query.Where(b => b.Gmail.Contains(Keyword) || b.BookCode.ToString().Contains(Keyword));
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }
    }
}