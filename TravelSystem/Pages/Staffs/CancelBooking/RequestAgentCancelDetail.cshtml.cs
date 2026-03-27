using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Staffs.CancelBooking
{
    public class RequestAgentCancelDetailModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public RequestAgentCancelDetailModel(FinalPrnContext context) => _context = context;

        // Đổi từ RequestCancel sang Booking
        public Booking BookingItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4 && roleId != 1) return RedirectToPage("/Auths/Login");

            // Lấy gốc từ bảng Bookings dựa trên id truyền vào
            BookingItem = await _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(td => td.Tour).ThenInclude(t => t.TravelAgent)
                .Include(b => b.User)
                .Include(b => b.RequestCancels) // Lấy kèm thông tin lý do hủy nếu có
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (BookingItem == null) return NotFound();

            return Page();
        }
    }
}