using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TravelSystemService.Services
{
    public class PayOSService
    {
        private readonly string _clientId = "e9654ff3-74c1-47f3-95a3-4374182de555";
        private readonly string _apiKey = "7f110b74-cf82-483b-b4d5-9e0725616a39";
        private readonly string _checksumKey = "efe3011fe6a87a5e7be4d32bcc001dd2bd6de461001ac49b6d784a65f949cddc";

        // --- HÀM 1: DÙNG CHO NẠP VÍ (AmountToPay) ---
        public async Task<string?> CreatePaymentLink(long orderCode, int amount, string description, string name, string email, string phone, string cancelUrl, string returnUrl)
        {
            try
            {
                long expiredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1800;
                var dataToSign = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                string signature = HashHmac(dataToSign, _checksumKey);

                var payload = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    description = description,
                    buyerName = name,
                    buyerEmail = email,
                    buyerPhone = phone,
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    expiredAt = expiredAt,
                    signature = signature
                };

                return await SendRequestToPayOS(payload);
            }
            catch (Exception) { return null; }
        }

        // --- HÀM 2: DÙNG CHO ĐẶT TOUR (BookTour) ---
        public async Task<string?> CreatePaymentLinkForTour(long orderCode, int amount, string description, string name, string email, string phone, string tourName, int quantity, string cancelUrl, string returnUrl)
        {
            try
            {
                long expiredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1800;
                var dataToSign = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                string signature = HashHmac(dataToSign, _checksumKey);

                var payload = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    description = description,
                    buyerName = name,
                    buyerEmail = email,
                    buyerPhone = phone,
                    items = new[]
                    {
                        new { name = tourName, quantity = quantity, price = amount }
                    },
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    expiredAt = expiredAt,
                    signature = signature
                };

                return await SendRequestToPayOS(payload);
            }
            catch (Exception) { return null; }
        }

        private async Task<string?> SendRequestToPayOS(object payload)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-client-id", _clientId);
            client.DefaultRequestHeaders.Add("x-api-key", _apiKey);

            var json = JsonSerializer.Serialize(payload);
            var response = await client.PostAsync("https://api-merchant.payos.vn/v2/payment-requests",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(content);
                // Đảm bảo lấy đúng đường dẫn data -> checkoutUrl
                if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    return dataElement.GetProperty("checkoutUrl").GetString();
                }
            }

            // Log lỗi chi tiết ra cửa sổ Output để Hùng dễ theo dõi
            System.Diagnostics.Debug.WriteLine("PayOS API Error Response: " + content);
            return null;
        }

        private string HashHmac(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            return BitConverter.ToString(hmac.ComputeHash(dataBytes)).Replace("-", "").ToLower();
        }
    }
}