using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Payment
{
    public class ReturnPaymentModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IEmailService _emailService;

        public ReturnPaymentModel(FinalPrnContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public Booking BookDetail { get; set; }
        public Tour Tour { get; set; }

        public async Task<IActionResult> OnGetAsync(long bookCode)
        {
            // 1. Lấy thông tin Booking và dùng Include để lấy hết data liên quan (TourDeparture, Tour)
            // Việc này giúp @Model.BookDetail?.TourDeparture?.StartDate không bị null ở file cshtml
            BookDetail = await _context.Bookings
                .Include(b => b.TourDeparture)
                    .ThenInclude(d => d.Tour)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookCode == bookCode);

            // Kiểm tra: Đơn hàng không tồn tại hoặc đã bị Hủy (Status = 0)
            if (BookDetail == null || BookDetail.Status == 0)
            {
                return RedirectToPage("/Payment/CancelPayment", new { bookCode = bookCode });
            }

            // Gán thông tin Tour để hiển thị ở dòng 16 trong file cshtml của bạn
            Tour = BookDetail.TourDeparture.Tour;

            // LUỒNG 1: Nếu thanh toán bằng Ví (Status đã là 1 từ trang BookTour)
            if (BookDetail.Status == 1)
            {
                return Page(); // Hiển thị trang thành công luôn
            }

            // LUỒNG 2: Nếu thanh toán qua PayOS (Status đang là 7 - Chờ xử lý)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Cập nhật trạng thái thành Thành công
                    BookDetail.Status = 1;

                    // Cập nhật số chỗ trống của Tour
                    int totalPeople = (BookDetail.NumberAdult ?? 0) + (BookDetail.NumberChildren ?? 0);
                    BookDetail.TourDeparture.AvailableSeat -= totalPeople;

                    decimal totalPrice = (decimal)(BookDetail.TotalPrice ?? 0);

                    // GHI LOG CHO TOURIST (Để khách xem trong lịch sử ví)
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = BookDetail.UserId,
                        Amount = totalPrice,
                        TransactionType = "PURCHASE",
                        Description = $"Thanh toán tour {Tour.TourName} (Mã: {BookDetail.BookCode})",
                        TransactionDate = DateTime.Now
                    });

                    // CẬP NHẬT VÍ VÀ GHI LOG CHO HỆ THỐNG (Mặc định Admin ID là 6)
                    var systemWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == 6);
                    if (systemWallet != null)
                    {
                        systemWallet.Balance += totalPrice;

                        _context.TransactionHistories.Add(new TransactionHistory
                        {
                            UserId = 6,
                            Amount = totalPrice,
                            TransactionType = "RECEIVE",
                            Description = $"Nhận thanh toán PayOS từ khách {BookDetail.Gmail} cho tour {Tour.TourName}",
                            TransactionDate = DateTime.Now
                        });
                    }

                    // Giảm số lượng Voucher nếu có áp dụng
                    if (BookDetail.VoucherId != null)
                    {
                        var voucher = await _context.Vouchers.FindAsync(BookDetail.VoucherId);
                        if (voucher != null && voucher.Quantity > 0) voucher.Quantity -= 1;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Gửi mail xác nhận
                    string formattedPrice = string.Format("{0:N0}", BookDetail.TotalPrice);
                    await SendConfirmationEmail(BookDetail, Tour, formattedPrice);

                    return Page();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToPage("/Payment/CancelPayment", new { bookCode = bookCode });
                }
            }
        }

        private async Task SendConfirmationEmail(Booking book, Tour tour, string price)
        {
            string content = $@"
                <h3>Chào {book.FirstName} {book.LastName},</h3>
                <p>Bạn đã thanh toán thành công tour: <strong>{tour.TourName}</strong></p>
                <ul>
                    <li><b>Mã đơn hàng:</b> #{book.BookCode}</li>
                    <li><b>Ngày khởi hành:</b> {book.TourDeparture.StartDate?.ToString("dd/MM/yyyy")}</li>
                    <li><b>Số lượng:</b> {book.NumberAdult} người lớn, {book.NumberChildren} trẻ em</li>
                    <li><b>Tổng tiền:</b> {price} VNĐ</li>
                </ul>
                <p>Cảm ơn bạn đã tin tưởng dịch vụ của GoViet!</p>";

            await _emailService.SendAsync(book.Gmail, "Xác nhận đặt tour thành công", content);
        }
    }
}