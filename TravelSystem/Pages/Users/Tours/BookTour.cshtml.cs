using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;
using TravelSystemService.Services;

namespace TravelSystem.Pages.Users.Tours
{
    public class BookTourModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly PayOSService _payOSService;
        private readonly IEmailService _emailService;

        public BookTourModel(FinalPrnContext context, PayOSService payOSService, IEmailService emailService)
        {
            _context = context;
            _payOSService = payOSService;
            _emailService = emailService;
        }

        [BindProperty] public Booking BookingInput { get; set; }
        public Tour Tour { get; set; }
        public TourDeparture Departure { get; set; }
        public List<Voucher> Vouchers { get; set; }
        public Vat ActiveVat { get; set; }
        public User CurrentUser { get; set; } // Thêm để lấy thông tin ví

        public List<int?> UsedVoucherIds { get; set; }

        public async Task<IActionResult> OnGetAsync(int departureId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            // Load toàn bộ dữ liệu cần thiết cho giao diện
            await LoadDataAsync(departureId, userId.Value);

            if (Departure == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int departureId, int? voucherId, decimal finalAmount, int paymentMethodId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var departure = await _context.TourDepartures.Include(d => d.Tour).FirstOrDefaultAsync(d => d.DepartureId == departureId);
            var user = await _context.Users.FindAsync(userId);

            // Tạo BookCode chuẩn long như bên Java
            long bookCode = GenerateRandomBookCode();

            // 1. THANH TOÁN BẰNG VÍ
            if (paymentMethodId == 1)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var wallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == userId);
                    if (wallet == null || wallet.Balance < finalAmount)
                    {
                        ModelState.AddModelError("", "Số dư ví không đủ để thanh toán.");
                        await LoadDataAsync(departureId, userId.Value);
                        return Page();
                    }

                    wallet.Balance -= finalAmount;
                    var systemWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == 1);
                    if (systemWallet != null) systemWallet.Balance += finalAmount;

                    var booking = CreateBookingObject(userId.Value, departureId, voucherId, finalAmount, paymentMethodId, bookCode, 1);
                    _context.Bookings.Add(booking);

                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = userId,
                        Amount = finalAmount,
                        TransactionType = "PURCHASE",
                        Description = $"Thanh toán tour {departure.Tour.TourName}",
                        TransactionDate = DateTime.Now
                    });

                    departure.AvailableSeat -= (BookingInput.NumberAdult + BookingInput.NumberChildren);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _emailService.SendAsync(user.Gmail, "Xác nhận đặt Tour", $"Chúc mừng! Bạn đã đặt thành công tour {departure.Tour.TourName}. Tổng tiền: {finalAmount:N0} VNĐ");

                    // Đổi đường dẫn về trang ReturnPayment để hiển thị thông tin thành công đồng nhất
                    return RedirectToPage("/Payment/ReturnPayment", new { bookCode = bookCode });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToPage("/Error");
                }
            }
            // 2. THANH TOÁN QUA PAYOS
            else
            {
                // Tạo đơn hàng ở trạng thái 7 (Chờ thanh toán)
                var booking = CreateBookingObject(userId.Value, departureId, voucherId, finalAmount, paymentMethodId, bookCode, 7);
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Chuẩn hóa thông tin như bên Java
                string tourName = departure.Tour.TourName;
                int totalPeople = (BookingInput.NumberAdult ?? 0) + (BookingInput.NumberChildren ?? 0);
                string buyerFullName = $"{BookingInput.FirstName} {BookingInput.LastName}";
                string description = $"ThanhToan{userId}{bookCode.ToString().Substring(0, 4)}"; // Bỏ dấu gạch dưới

                string cancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/CancelPayment?bookCode={bookCode}";
                string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/ReturnPayment?bookCode={bookCode}";

                // Rút ngắn description để đảm bảo < 25 ký tự
                string safeDescription = $"Tour{bookCode.ToString().Substring(10)}";

                var checkoutUrl = await _payOSService.CreatePaymentLinkForTour(
                    bookCode,
                    (int)finalAmount,
                    safeDescription,
                    $"{BookingInput.FirstName} {BookingInput.LastName}",
                    BookingInput.Gmail,
                    BookingInput.Phone,
                    departure.Tour.TourName,
                    (BookingInput.NumberAdult ?? 0) + (BookingInput.NumberChildren ?? 0),
                    cancelUrl,
                    returnUrl);

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    ModelState.AddModelError("", "Không thể khởi tạo thanh toán với PayOS. Vui lòng kiểm tra lại thông tin.");
                    await LoadDataAsync(departureId, userId.Value);
                    return Page();
                }

                return Redirect(checkoutUrl);
            }
        }

        // Hàm helper để load data tránh lặp code
        private async Task LoadDataAsync(int departureId, int userId)
        {
            // 1. Lấy thông tin Tour và Đại lý sở hữu tour đó
            Departure = await _context.TourDepartures
                .Include(d => d.Tour)
                    .ThenInclude(t => t.TravelAgent)
                .FirstOrDefaultAsync(d => d.DepartureId == departureId);

            if (Departure != null) Tour = Departure.Tour;
            CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            // 2. Lấy danh sách VoucherId mà User đã dùng trong các đơn hàng THÀNH CÔNG (Status 1, 4)
            // Tránh việc đơn bị hủy rồi mà vẫn không cho dùng lại voucher.
            UsedVoucherIds = await _context.Bookings
                .Where(b => b.UserId == userId && b.VoucherId != null && (b.Status == 1 || b.Status == 4))
                .Select(b => b.VoucherId)
                .Distinct()
                .ToListAsync();

            // 3. Lấy danh sách Voucher thỏa mãn
            Vouchers = await _context.Vouchers
                .Where(v => v.Quantity > 0
                       && v.EndDate >= DateOnly.FromDateTime(DateTime.Now)
                       && v.StartDate <= DateOnly.FromDateTime(DateTime.Now))
                .Where(v =>
                    // Loại 1: Của Staff (Status 1) và khách chưa dùng
                    (v.Status == 1 && !UsedVoucherIds.Contains(v.VoucherId)) ||
                    // Loại 2: Của chính Agent sở hữu tour này (Status 2)
                    (v.Status == 2 && v.UserId == Tour.TravelAgent.UserId)
                )
                .ToListAsync();

            ActiveVat = await _context.Vats.FirstOrDefaultAsync(v => v.Status == 1);
        }

        private Booking CreateBookingObject(int userId, int departureId, int? voucherId, decimal total, int payMethod, long code, int status)
        {
            return new Booking
            {
                UserId = userId,
                TourDepartureId = departureId,
                VoucherId = voucherId,
                BookDate = DateOnly.FromDateTime(DateTime.Now),
                NumberAdult = BookingInput.NumberAdult,
                NumberChildren = BookingInput.NumberChildren,
                FirstName = BookingInput.FirstName,
                LastName = BookingInput.LastName,
                Phone = BookingInput.Phone,
                Gmail = BookingInput.Gmail,
                Note = BookingInput.Note,
                TotalPrice = (double)total,
                PaymentMethodId = payMethod,
                Status = status,
                BookCode = code
            };
        }

        private long GenerateRandomBookCode() => 100000000000L + (long)(new Random().NextDouble() * (9007199254740991L - 100000000000L));
    }
}