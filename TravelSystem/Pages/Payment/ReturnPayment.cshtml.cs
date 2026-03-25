using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.BookTours
{
    public class ReturnPaymentModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public ReturnPaymentModel(FinalPrnContext context)
        {
            _context = context;
        }

        public BookDetail BookDetail { get; set; }
        public Tour Tour { get; set; }
        public string DisplayMessage { get; set; }

        // Nhận tham số: bookCode (từ cả 2), status (từ PayOS), method (từ Wallet)
        public async Task<IActionResult> OnGetAsync(long bookCode, string status, string method)
        {
            // 1. Tìm thông tin đặt tour kèm theo Tour data
            BookDetail = await _context.BookDetails
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.BookCode == bookCode);

            if (BookDetail == null)
            {
                return RedirectToPage("/Index");
            }

            Tour = BookDetail.Tour;

            // 2. Kiểm tra nguồn thanh toán
            if (method == "wallet")
            {
                // Thanh toán ví đã xử lý trừ tiền ở trang trước, ở đây chỉ hiển thị
                DisplayMessage = "Bạn đã thanh toán thành công bằng ví GoViet";
                // Đảm bảo status là 1 (Đã thanh toán)
                if (BookDetail.Status != 1)
                {
                    BookDetail.Status = 1;
                    await _context.SaveChangesAsync();
                }
            }
            else if (status == "PAID" || status == "NOPAY")
            {
                // Thanh toán PayOS thành công
                DisplayMessage = "Bạn đã thanh toán thành công đơn hàng qua PayOS";
                BookDetail.Status = 1; // Gán về 1 (Đã thanh toán) thay vì 2
                await _context.SaveChangesAsync();
            }
            else if (status == "CANCELLED")
            {
                // Người dùng bấm hủy trên cổng PayOS
                return RedirectToPage("/Payment/CancelPayment");
            }
            else
            {
                // Trường hợp nạp tiền hoặc lỗi không xác định
                DisplayMessage = "Thông tin chi tiết đơn hàng";
            }

            return Page();
        }
    }
}