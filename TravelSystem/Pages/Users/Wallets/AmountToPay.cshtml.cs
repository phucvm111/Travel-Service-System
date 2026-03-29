using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystemService.Services;

namespace TravelSystem.Pages.Wallet
{
    public class AmountToPayModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly PayOSService _payOSService;

        public AmountToPayModel(FinalPrnContext context, PayOSService payOSService)
        {
            _context = context;
            _payOSService = payOSService;
        }

        // Data hiển thị
        public User CurrentUser { get; set; } // Thêm cái này để dùng ở giao diện
        public decimal UserWallet { get; set; }
        public List<TransactionHistory> Transactions { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SelectedType { get; set; }

        public async Task<IActionResult> OnGetAsync(string type, int p = 1)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            CurrentUser = await _context.Users.FindAsync(userId);
            SelectedType = type;

            var query = _context.TransactionHistories
                .Where(t => t.UserId == userId)
                .AsQueryable();

            // Lọc theo loại giao dịch nếu có
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(t => t.TransactionType == type);
            }

            // --- CẤU HÌNH PHÂN TRANG ---
            int pageSize = 5; // Hiển thị 5 bản ghi mỗi trang theo yêu cầu của bạn
            int totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại không nhỏ hơn 1 và không lớn hơn tổng số trang
            CurrentPage = p < 1 ? 1 : (p > TotalPages && TotalPages > 0 ? TotalPages : p);

            Transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDepositAsync(int amount)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToPage("/Auths/Login");

            // 1. Tạo mã đơn hàng và mô tả
            long orderCode = long.Parse(userId.Value.ToString() + DateTime.Now.ToString("ddHHmmss"));
            string description = $"Nap_User{userId}_{Guid.NewGuid().ToString().Substring(0, 4)}";

            // 2. TẠO BẢN GHI FUNDREQUEST Ở TRẠNG THÁI PENDING
            var pendingRequest = new FundRequest
            {
                CreateBy = userId,
                Amount = amount,
                RequestDate = DateTime.Now,
                ReferenceCode = description, // Dùng description làm mã tham chiếu để đối soát sau này
                Status = "pending",
                Type = "RECHARGE",
                BankNameSnapshot = "PayOS Online"
            };

            _context.FundRequests.Add(pendingRequest);
            await _context.SaveChangesAsync(); // Lưu để lấy ID nếu cần

            // 3. Cấu hình URL trả về
            string cancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/WalletPaymentFalse?referenceCode={description}&amount={amount}";
            string returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/WalletPaymentSuccess?referenceCode={description}&amount={amount}";

            // 4. Gọi PayOS Service
            var checkoutUrl = await _payOSService.CreatePaymentLink(
                orderCode,
                amount,
                description,
                $"{user.FirstName} {user.LastName}",
                user.Gmail,
                user.Phone,
                cancelUrl,
                returnUrl);

            if (!string.IsNullOrEmpty(checkoutUrl))
            {
                return Redirect(checkoutUrl);
            }

            ModelState.AddModelError("", "Lỗi kết nối với cổng thanh toán PayOS.");
            return await OnGetAsync(null);
        }
    }
}