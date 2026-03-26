using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Wallet
{
    public class DepositSuccessModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DepositSuccessModel(FinalPrnContext context)
        {
            _context = context;
        }

        public User Customer { get; set; }
        public decimal Amount { get; set; }

        public async Task<IActionResult> OnGetAsync(string referenceCode, decimal amount)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tìm bản ghi FundRequest đang ở trạng thái pending
                    var fundRequest = await _context.FundRequests
                        .FirstOrDefaultAsync(f => f.ReferenceCode == referenceCode && f.Status == "pending");

                    if (fundRequest == null)
                    {
                        // Có thể giao dịch đã được xử lý hoặc không tồn tại
                        return RedirectToPage("/Wallet/AmountToPay");
                    }

                    // 2. Cập nhật trạng thái FundRequest
                    fundRequest.Status = "approved";
                    fundRequest.ApprovedDate = DateTime.Now;
                    fundRequest.ApproveBy = 1; // Admin ID

                    // 3. Cộng tiền vào ví (Khách và Hệ thống)
                    var touristWallet = await _context.Wallets.FindAsync(userId);
                    var systemWallet = await _context.Wallets.FindAsync(6); // Ví hệ thống ID 6

                    touristWallet.Balance += amount;
                    systemWallet.Balance += amount;

                    // 4. Thêm Transaction History
                    var history = new TransactionHistory
                    {
                        UserId = userId,
                        Amount = amount,
                        TransactionType = "RECHARGE",
                        Description = $"Nạp tiền thành công qua PayOS (Ref: {referenceCode})",
                        TransactionDate = DateTime.Now
                    };
                    _context.TransactionHistories.Add(history);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Customer = await _context.Users.FindAsync(userId); // Lấy thông tin user để hiện tên/email
                    Amount = amount; // Gán số tiền nạp để hiện lên giao diện

                    return Page(); // Hiển thị trang nạp thành công
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToPage("/Payment/WalletPaymentFalse");
                }
            }
        }
    }
}