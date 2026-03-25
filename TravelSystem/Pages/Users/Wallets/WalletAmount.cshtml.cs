using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TravelSystem.Models;

namespace TravelSystem.Pages.Users.Wallets
{
    public class WalletAmountModel : PageModel
    {
        private readonly FinalPrnContext _con;

        public WalletAmountModel(FinalPrnContext con)
        {
            _con = con;
        }

        public TravelSystem.Models.Wallet Wallet { get; set; }
        public User User { get; set; }
        public List<TransactionHistory> TransactionHistoryList { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty]
        public int Amount { get; set; }
        public string Error { get; set; }

        // Hàm dùng chung để load dữ liệu cho cả GET và POST (khi POST lỗi)
        private void LoadPageData(int page = 1, string type = "")
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return;

            User = _con.Users.FirstOrDefault(u => u.UserId == userId);
            Wallet = _con.Wallets.FirstOrDefault(w => w.UserId == userId);

            var query = _con.TransactionHistories.Where(t => t.UserId == userId);
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(t => t.TransactionType == type);
            }

            var allTransactions = query.OrderByDescending(t => t.TransactionDate).ToList();
            int pageSize = 5;
            TotalPages = (int)Math.Ceiling((double)allTransactions.Count / pageSize);
            if (TotalPages == 0) TotalPages = 1;

            CurrentPage = (page < 1) ? 1 : (page > TotalPages ? TotalPages : page);

            TransactionHistoryList = allTransactions
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public IActionResult OnGet(string type, int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            LoadPageData(page, type);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            User loginUser = _con.Users.FirstOrDefault(u => u.UserId == userId);

            if (loginUser == null) return RedirectToPage("/Auths/Login");

            try
            {
                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                int amount = Amount;
                long paymentCode = GenerateRandomBookCode();

                // PayOS giới hạn description 25 ký tự, nên rút gọn lại
                string descriptionPayment = $"Nap tien {userId}";

                // Quan trọng: Truyền referenceCode vào returnUrl để trang Success có thể tìm thấy record
                string returnUrl = $"{baseUrl}/Payment/PaymentWalletSuccess?referenceCode={paymentCode}";
                string cancelUrl = $"{baseUrl}/Payment/PaymentWalletCancel";

                PendingRecharge pendingRecharge = new PendingRecharge
                {
                    Amount = amount,
                    UserId = loginUser.UserId,
                    ReferenceCode = paymentCode.ToString(),
                    RequestDate = DateTime.Now,
                    Status = "PENDING",
                    PaymentMethodId = 2
                };

                _con.PendingRecharges.Add(pendingRecharge);
                int check = _con.SaveChanges();

                if (check > 0)
                {
                    string checkoutUrl = await PayOS(
                        paymentCode,
                        amount,
                        descriptionPayment,
                        $"{loginUser.FirstName} {loginUser.LastName}",
                        loginUser.Gmail,
                        loginUser.Phone,
                        cancelUrl,
                        returnUrl
                    );

                    if (!string.IsNullOrEmpty(checkoutUrl))
                    {
                        return Redirect(checkoutUrl);
                    }
                    Error = "Không thể khởi tạo link thanh toán.";
                }
            }
            catch (Exception ex)
            {
                Error = "Lỗi: " + ex.Message;
            }

            // Nếu thất bại, nạp lại dữ liệu trang thay vì gọi OnGet
            LoadPageData();
            return Page();
        }

        #region PayOS Helpers
        public static long GenerateRandomBookCode()
        {
            Random random = new Random();
            return (long)(random.NextDouble() * (9007199254740991L - 100000000000L)) + 100000000000L;
        }

        public static string GenerateSignature(string data, string checksumKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(checksumKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        public static async Task<string> PayOS(long paymentCode, int amount, string descriptionPayment, string name, string email, string phone, string cancelUrl, string returnUrl)
        {
            try
            {
                long expiredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1800;
                // Chuỗi data phải đúng thứ tự alphabet của key để khớp signature
                string data = $"amount={amount}&cancelUrl={cancelUrl}&description={descriptionPayment}&orderCode={paymentCode}&returnUrl={returnUrl}";
                string checksumKey = "efe3011fe6a87a5e7be4d32bcc001dd2bd6de461001ac49b6d784a65f949cddc";
                string signature = GenerateSignature(data, checksumKey);

                var requestBody = new
                {
                    orderCode = paymentCode,
                    amount = amount,
                    description = descriptionPayment,
                    buyerName = name,
                    buyerEmail = email,
                    buyerPhone = phone,
                    items = new[] { new { name = "Nap tien vi", quantity = 1, price = amount } },
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    expiredAt = expiredAt,
                    signature = signature
                };

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-client-id", "e9654ff3-74c1-47f3-95a3-4374182de555");
                client.DefaultRequestHeaders.Add("x-api-key", "7f110b74-cf82-483b-b4d5-9e0725616a39");

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content);
                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    return dataElement.GetProperty("checkoutUrl").GetString();
                }
            }
            catch { }
            return null;
        }
        #endregion
    }
}