using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Agents.TourBooked
{
    public class ManageTourBookedModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IEmailService _emailService;

        public ManageTourBookedModel(Prn222PrjContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public List<BookDetail> ListBookDetail { get; set; } = new();
        public int TotalPages { get; set; }
        public int StartIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusType { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agent == null) return RedirectToPage("/Index");

            const int pageSize = 5;

            var query = _context.BookDetails
                .Include(b => b.Tour)
                .Include(b => b.Voucher)
                .Where(b => b.Tour.TravelAgentId == agent.TravelAgentId)
                .AsQueryable();

            // 1. Filter Logic
            if (!string.IsNullOrEmpty(SearchName))
            {
                var search = SearchName.Trim().ToLower();
                query = query.Where(b => (b.FirstName + " " + b.LastName).ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(StatusType) && int.TryParse(StatusType, out int status))
            {
                query = query.Where(b => b.Status == status);
            }

            // 2. Pagination Calculation
            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Bọc bảo vệ: Nếu PageNumber vượt quá tổng số trang sau khi lọc
            if (PageNumber < 1) PageNumber = 1;
            if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

            StartIndex = (PageNumber - 1) * pageSize + 1;

            // 3. Execution
            ListBookDetail = await query
                .OrderByDescending(b => b.BookDate)
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int bookID, string reason)
        {
            var booking = await _context.BookDetails
                .Include(b => b.Tour)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BookId == bookID);

            if (booking == null) return RedirectToPage(new { cancel = "false" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                booking.Status = 3; // Hủy bởi đại lý
                booking.Note = "Đại lý hủy với lý do: " + reason;

                decimal refundAmount = (decimal)(booking.TotalPrice ?? 0);
                if (refundAmount > 0)
                {
                    var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == booking.UserId);
                    if (userWallet != null)
                    {
                        userWallet.Balance += refundAmount;
                        _context.TransactionHistories.Add(new TransactionHistory
                        {
                            UserId = booking.UserId ?? 0,
                            Amount = refundAmount,
                            TransactionType = "REFUND",
                            Description = $"Đại lý hủy tour: {booking.Tour?.TourName}",
                            TransactionDate = DateTime.Now,
                            AmountAfterTransaction = userWallet.Balance
                        });
                    }
                }

                if (booking.Tour != null)
                {
                    booking.Tour.Quantity += (booking.NumberAdult ?? 0) + (booking.NumberChildren ?? 0);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    string subject = "Thông báo: Chuyến đi đã bị hủy bởi Đại lý";
                    string content = $@"<h3>Chào {booking.FirstName},</h3>
                                      <p>Tour <b>{booking.Tour?.TourName}</b> đã bị đại lý hủy.</p>
                                      <p><b>Lý do:</b> {reason}</p>
                                      <p><b>Hoàn tiền:</b> {refundAmount:N0} VNĐ vào ví GoViet.</p>";
                    await _emailService.SendAsync(booking.Gmail, subject, content, true);
                }
                catch { /* Silent log */ }

                return RedirectToPage(new { cancel = "true" });
            }
            catch
            {
                await transaction.RollbackAsync();
                return RedirectToPage(new { cancel = "false" });
            }
        }
    }
}