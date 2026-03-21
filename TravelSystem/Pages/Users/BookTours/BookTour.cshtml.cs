using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TravelSystem.Pages.Users.BookTours
{
    public class BookTourModel : PageModel
    {
        private readonly Prn222PrjContext _con;
        private readonly IEmailService _emailService;

        public BookTourModel(Prn222PrjContext con, IEmailService emailService)
        {
            _con = con;
            _emailService = emailService;
        }

        public Tour Tour { get; set; }
        public Vat ActiveVat { get; set; }
        public List<Voucher> VoucherList { get; set; }

        [BindProperty] public string FirstName { get; set; }
        [BindProperty] public string LastName { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Phone { get; set; }
        [BindProperty] public int Adult { get; set; } = 1;
        [BindProperty] public int Children { get; set; } = 0;
        [BindProperty] public string Note { get; set; }
        [BindProperty] public int IsOther { get; set; } // Khớp với int? trong Model
        [BindProperty] public int PaymentMethodID { get; set; }
        [BindProperty] public int? SelectedVoucherId { get; set; }
        [BindProperty] public double FinalAmount { get; set; } // Đổi sang double cho khớp Model

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int tourId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            await LoadData(tourId);
            if (Tour == null) return NotFound();
            return Page();
        }

        private async Task LoadData(int tourId)
        {
            Tour = await _con.Tours.FirstOrDefaultAsync(t => t.TourId == tourId);
            ActiveVat = await _con.Vats.FirstOrDefaultAsync(v => v.Status == 1);

            var today = DateOnly.FromDateTime(DateTime.Now);
            VoucherList = await _con.Vouchers
                .Where(v => v.Status == 1 && v.EndDate >= today)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(int tourId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            await LoadData(tourId);

            // 1. Validate sơ bộ
            if (string.IsNullOrEmpty(FirstName)) ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
            if (!string.IsNullOrEmpty(ErrorMessage)) return Page();

            long bookCode = GenerateRandomBookCode();
            string description = $"PayTour{bookCode}".Substring(0, Math.Min(20, $"PayTour{bookCode}".Length));

            // 2. Khởi tạo đối tượng dựa đúng theo Model BookDetail bạn gửi
            var newBooking = new BookDetail
            {
                UserId = userId,
                TourId = tourId,
                VoucherId = SelectedVoucherId,
                BookDate = DateOnly.FromDateTime(DateTime.Now), // Lưu ngày đặt
                NumberAdult = Adult,
                NumberChildren = Children,
                FirstName = FirstName,
                LastName = LastName,
                Phone = Phone,
                Gmail = Email,
                Note = Note,
                IsBookedForOther = IsOther, // Sử dụng kiểu int (0 hoặc 1)
                TotalPrice = FinalAmount,
                BookCode = bookCode,
                PaymentMethodId = PaymentMethodID,
                Status = (PaymentMethodID == 1) ? 1 : 7 // 1: Đã thanh toán (Ví), 7: Chờ (PayOS)
            };

            // 3. Xử lý Logic
            if (PaymentMethodID == 1) // Thanh toán bằng Ví
            {
                var wallet = await _con.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null || wallet.Balance < (decimal)FinalAmount)
                {
                    ErrorMessage = "Số dư ví không đủ để thanh toán.";
                    return Page();
                }

                // Trừ tiền và lưu đơn hàng
                wallet.Balance -= (decimal)FinalAmount;
                _con.BookDetails.Add(newBooking);

                // Cập nhật số lượng chỗ còn lại của Tour
                if (Tour.Quantity >= (Adult + Children))
                {
                    Tour.Quantity -= (Adult + Children);
                }

                await _con.SaveChangesAsync();

                string mailBody = $"Xác nhận đặt tour {Tour.TourName} thành công. Tổng tiền: {FinalAmount:N0} VNĐ";
                await _emailService.SendAsync(Email, "GoViet - Xác nhận đặt tour", mailBody, true);

                // Trong file BookTour.cshtml.cs đoạn thanh toán ví
                return RedirectToPage("/Payment/ReturnPayment", new { bookCode = bookCode, method = "wallet" });
            }
            else // Thanh toán PayOS
            {
                // Lưu nháp vào DB ở trạng thái Chờ thanh toán (Status 7)
                _con.BookDetails.Add(newBooking);
                await _con.SaveChangesAsync();

                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string cancelUrl = $"{baseUrl}/Payment/CancelPayment";
                string returnUrl = $"{baseUrl}/Payment/ReturnPayment?bookCode={bookCode}";

                string checkoutUrl = await CreatePayOSLink(bookCode, (decimal)FinalAmount, description, $"{FirstName} {LastName}", Email, Phone, Tour.TourName, Adult + Children, cancelUrl, returnUrl);

                if (checkoutUrl != null) return Redirect(checkoutUrl);
            }

            ErrorMessage = "Lỗi kết nối thanh toán. Vui lòng thử lại.";
            return Page();
        }

        private long GenerateRandomBookCode() => (long)(new Random().NextDouble() * (9007199254740991L - 100000000000L)) + 100000000000L;

        private async Task<string> CreatePayOSLink(long code, decimal amount, string desc, string buyer, string email, string phone, string tourName, int qty, string cancel, string @return)
        {
            try
            {
                var checksumKey = "efe3011fe6a87a5e7be4d32bcc001dd2bd6de461001ac49b6d784a65f949cddc";
                int amountInt = (int)amount;
                // PayOS bắt buộc thứ tự Alphabet: amount -> cancelUrl -> description -> orderCode -> returnUrl
                string data = $"amount={amountInt}&cancelUrl={cancel}&description={desc}&orderCode={code}&returnUrl={@return}";

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
                var signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();

                var payload = new
                {
                    orderCode = code,
                    amount = amountInt,
                    description = desc,
                    buyerName = buyer,
                    buyerEmail = email,
                    buyerPhone = phone,
                    items = new[] { new { name = tourName.Length > 20 ? tourName.Substring(0, 20) : tourName, quantity = qty, price = amountInt } },
                    cancelUrl = cancel,
                    returnUrl = @return,
                    expiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds(),
                    signature = signature
                };

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-client-id", "e9654ff3-74c1-47f3-95a3-4374182de555");
                client.DefaultRequestHeaders.Add("x-api-key", "7f110b74-cf82-483b-b4d5-9e0725616a39");

                var response = await client.PostAsJsonAsync("https://api-merchant.payos.vn/v2/payment-requests", payload);
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (result.GetProperty("code").GetString() == "00")
                    return result.GetProperty("data").GetProperty("checkoutUrl").GetString();
            }
            catch { return null; }
            return null;
        }
    }
}