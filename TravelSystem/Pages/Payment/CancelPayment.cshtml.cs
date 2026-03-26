using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Payment
{
    public class CancelPaymentModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public CancelPaymentModel(FinalPrnContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(long? bookCode)
        {
            // Nếu có mã đơn hàng, cập nhật trạng thái về 0 (Hủy) để giải phóng các đơn treo
            if (bookCode.HasValue)
            {
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookCode == bookCode);
                if (booking != null && booking.Status == 7) // Chỉ hủy nếu đang ở trạng thái chờ thanh toán
                {
                    booking.Status = 0; // 0 = Canceled
                    await _context.SaveChangesAsync();
                }
            }
            return Page();
        }
    }
}