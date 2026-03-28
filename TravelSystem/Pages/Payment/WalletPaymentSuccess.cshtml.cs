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
                    // 1. Tìm bản ghi FundRequest
                    var fundRequest = await _context.FundRequests
                        .FirstOrDefaultAsync(f => f.ReferenceCode == referenceCode && f.Status == "pending");

                    if (fundRequest == null) return RedirectToPage("/Wallet/AmountToPay");

                    // 2. Cập nhật trạng thái FundRequest
                    fundRequest.Status = "approved";
                    fundRequest.ApprovedDate = DateTime.Now;
                    fundRequest.ApproveBy = 1;

                    // 3. Lấy ví (Hùng lưu ý ví hệ thống ID là 1 hay 6 nhé, trong SQL bạn gửi Admin là 1)
                    var touristWallet = await _context.Users.FindAsync(userId);
                    var systemWallet = await _context.Users.FindAsync(1); // Trong DB mới Admin ID là 1

                    if (touristWallet == null || systemWallet == null) throw new Exception("Ví không tồn tại");

                    // Cộng tiền vào cả 2 ví
                    touristWallet.Balance += amount;
                    systemWallet.Balance += amount;

                    // 4. Ghi Log lịch sử giao dịch cho KHÁCH
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = userId,
                        Amount = amount,
                        TransactionType = "RECHARGE",
                        Description = $"Nạp tiền qua PayOS (Ref: {referenceCode}) | [Số dư: {touristWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    // 5. Ghi Log lịch sử giao dịch cho HỆ THỐNG (BỔ SUNG TẠI ĐÂY)
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = 1, // ID của Admin/Hệ thống
                        Amount = amount,
                        TransactionType = "DEPOSIT", // Hoặc "INCOME" tùy bạn quy định
                        Description = $"Khách {userId} nạp tiền qua PayOS | [Ví tổng: {systemWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Customer = await _context.Users.FindAsync(userId);
                    Amount = amount;

                    return Page();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return RedirectToPage("/Payment/WalletPaymentFalse");
                }
            }
        }
    }
}