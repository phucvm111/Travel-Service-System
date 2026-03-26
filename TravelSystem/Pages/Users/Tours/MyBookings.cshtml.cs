using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Tours
{
    public class MyBookingsModel : PageModel
    {
        private readonly FinalPrnContext _context;
        public MyBookingsModel(FinalPrnContext context) => _context = context;

        public List<Booking> BookingList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Filter { get; set; }

        public async Task<IActionResult> OnGetAsync(string filter = "current", int p = 1)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Filter = filter;
            CurrentPage = p;
            int pageSize = 5;

            var query = _context.Bookings
                .Include(b => b.TourDeparture).ThenInclude(d => d.Tour)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookDate)
                .AsQueryable();

            // Logic lọc theo tab (Giống logic Java của bạn)
            if (filter == "current")
            {
                // Chuyến đi sắp tới: Status 1 (Đã thanh toán), 7 (Chờ thanh toán), 5 (Đang hoàn tiền)
                query = query.Where(b => b.Status == 1 || b.Status == 7 || b.Status == 5);
            }
            else
            {
                // Chuyến đi đã xong: Status 4 (Hoàn thành), 2 (Hủy), 3 (Đại lý hủy), 6 (Đã hoàn tiền)
                query = query.Where(b => b.Status == 4 || b.Status == 2 || b.Status == 3 || b.Status == 6);
            }

            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            BookingList = await query.Skip((CurrentPage - 1) * pageSize).Take(pageSize).ToListAsync();

            return Page();
        }

        // Handler cho việc gửi Feedback (AddFeedback)
        public async Task<IActionResult> OnPostFeedbackAsync(int bookId, int rate, string content, IFormFile image)
        {
            var booking = await _context.Bookings.FindAsync(bookId);
            if (booking == null) return NotFound();

            string imagePath = null;
            if (image != null)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/feedbacks", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                imagePath = "uploads/feedbacks/" + fileName;
            }

            var feedback = new Feedback
            {
                BookId = bookId,
                Rate = rate,
                Content = content,
                Image = imagePath,
                CreateDate = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { filter = "finished", success = "Đã gửi đánh giá thành công!" });
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookId, string reason)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var booking = await _context.Bookings.FindAsync(bookId);
            if (booking == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo yêu cầu hủy trong bảng RequestCancel (giống RequestCancelDAO)
                var cancelRequest = new RequestCancel
                {
                    BookId = bookId,
                    RequestDate = DateOnly.FromDateTime(DateTime.Now),
                    Reason = reason,
                    Status = "PENDING" // Trạng thái yêu cầu chờ Staff duyệt
                };
                _context.RequestCancels.Add(cancelRequest);

                // 2. Cập nhật trạng thái Booking thành 5 (Đã yêu cầu hoàn tiền/hủy)
                booking.Status = 5;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToPage(new { filter = "current", cancelSuccess = "true" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return RedirectToPage(new { filter = "current", cancelSuccess = "false" });
            }
        }
    }
}