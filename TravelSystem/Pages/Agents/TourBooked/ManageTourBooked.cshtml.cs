using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.TourBooked
{
    public class ManageTourBookedModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public ManageTourBookedModel(FinalPrnContext context) => _context = context;

        public List<Booking> BookingList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int StartIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? StatusType { get; set; }

        public async Task<IActionResult> OnGetAsync(int p = 1)
        {
            // 1. Kiểm tra quyền Agent (RoleId = 3)
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            int? agentUserId = HttpContext.Session.GetInt32("UserID");
            if (roleId != 3 || agentUserId == null) return RedirectToPage("/Auths/Login");

            // 2. Lấy TravelAgentID của User hiện tại
            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == agentUserId);
            if (agent == null) return NotFound("Thông tin đại lý không tồn tại.");

            CurrentPage = p;
            int pageSize = 5;

            // 3. Xây dựng truy vấn
            var query = _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .Include(b => b.Voucher)
                .Where(b => b.TourDeparture.Tour.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            // Lọc theo tên khách hàng
            if (!string.IsNullOrEmpty(SearchName))
            {
                string lowerSearch = SearchName.ToLower();
                query = query.Where(b => (b.FirstName + " " + b.LastName).ToLower().Contains(lowerSearch));
            }

            // Lọc theo trạng thái
            if (StatusType.HasValue)
            {
                query = query.Where(b => b.Status == StatusType.Value);
            }

            // 4. Phân trang
            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            StartIndex = (CurrentPage - 1) * pageSize + 1;

            BookingList = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        // Action xử lý Đại lý chủ động hủy tour
        public async Task<IActionResult> OnPostCancelAsync(int bookId, string reason)
        {
            var booking = await _context.Bookings.FindAsync(bookId);
            if (booking == null) return NotFound();

            // Cập nhật trạng thái thành 3: Đại lý yêu cầu hủy
            booking.Status = 3;
            booking.Note = $"[Đại lý hủy]: {reason} | " + booking.Note;

            await _context.SaveChangesAsync();
            return RedirectToPage(new { cancel = "true" });
        }
    }
}