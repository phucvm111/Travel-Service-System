using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.BookTours
{
    public class ReturnPaymentModel : PageModel
    {
        private readonly Prn222PrjContext _context;

        public ReturnPaymentModel(Prn222PrjContext context)
        {
            _context = context;
        }

        public BookDetail BookDetail { get; set; }
        public Tour Tour { get; set; }

        public async Task<IActionResult> OnGetAsync(long bookCode)
        {
            // Tìm thông tin đặt tour dựa trên mã bookCode truyền từ URL về
            BookDetail = await _context.BookDetails
                .FirstOrDefaultAsync(b => b.BookCode == bookCode);

            if (BookDetail != null)
            {
                // Sau khi thanh toán PayOS thành công, cập nhật trạng thái đơn hàng (ví dụ: status 2 là đã thanh toán)
                BookDetail.Status = 2;

                Tour = await _context.Tours
                    .FirstOrDefaultAsync(t => t.TourId == BookDetail.TourId);

                await _context.SaveChangesAsync();
            }

            return Page();
        }
    }
}