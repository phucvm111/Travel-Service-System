using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Staff
{
    public class WithdrawDetailModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IEmailService _emailService;

        public WithdrawDetailModel(Prn222PrjContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public WithdrawRequest Withdraw { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Withdraw = await _context.WithdrawRequests.FirstOrDefaultAsync(m => m.Id == id);

            if (Withdraw == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string action, string reason)
        {
            var adminId = HttpContext.Session.GetInt32("UserID");
            if (adminId == null) return RedirectToPage("/Auths/Login");

            var request = await _context.WithdrawRequests.FindAsync(id);
            if (request == null || request.Status != "PENDING") return RedirectToPage("./WithdrawManagement");

            if (action == "reject")
            {
                if (string.IsNullOrEmpty(reason))
                {
                    ErrorMessage = "Vui lòng nhập lý do từ chối.";
                    Withdraw = request;
                    return Page();
                }

                request.Status = "REJECTED";
                await _context.SaveChangesAsync();

                await SendRejectEmail(request.Email, reason);
            }
            else if (action == "approve")
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var userWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
                    var systemWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == 6); // Ví hệ thống

                    if (userWallet == null || systemWallet == null || userWallet.Balance < request.Amount)
                    {
                        ErrorMessage = "Số dư không đủ để thực hiện thao tác này.";
                        Withdraw = request;
                        return Page();
                    }

                    // 1. Trừ tiền ví User và ví Hệ thống
                    userWallet.Balance -= (decimal)request.Amount;
                    systemWallet.Balance -= (decimal)request.Amount;

                    // 2. Lưu lịch sử giao dịch cho User
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = request.UserId,
                        Amount = (decimal)request.Amount,
                        TransactionType = "WITHDRAW",
                        Description = "Bạn đã rút tiền từ tài khoản.",
                        AmountAfterTransaction = userWallet.Balance,
                        TransactionDate = DateTime.Now
                    });

                    // 3. Lưu lịch sử giao dịch cho Hệ thống
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        UserId = 6,
                        Amount = (decimal)request.Amount,
                        TransactionType = "PURCHASE",
                        Description = $"Người dùng {request.Email} rút tiền.",
                        AmountAfterTransaction = systemWallet.Balance,
                        TransactionDate = DateTime.Now
                    });

                    // 4. Cập nhật trạng thái yêu cầu
                    request.Status = "APPROVED";
                    request.ApprovedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await SendApproveEmail(request);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    ErrorMessage = "Có lỗi xảy ra trong quá trình xử lý.";
                    Withdraw = request;
                    return Page();
                }
            }

            return RedirectToPage("./WithdrawManagement");
        }

        private async Task SendRejectEmail(string email, string reason)
        {
            string subject = "Yêu cầu rút tiền bị từ chối";
            string content = $@"<h3>Chào bạn,</h3>
                <p>Yêu cầu rút tiền của bạn đã bị từ chối.</p>
                <p><b>Lý do:</b> {reason}</p>
                <p>Trân trọng,<br>Đội ngũ GoViet</p>";
            await _emailService.SendAsync(email, subject, content, true);
        }

        private async Task SendApproveEmail(WithdrawRequest req)
        {
            string subject = "Yêu cầu rút tiền thành công";
            string content = $@"<h3>Chào bạn,</h3>
                <p>Bạn đã rút thành công số tiền: <b>{req.Amount:N0} VNĐ</b></p>
                <p>Tiền đã được chuyển về tài khoản ngân hàng của bạn.</p>
                <p>Trân trọng,<br>Đội ngũ GoViet</p>";
            await _emailService.SendAsync(req.Email, subject, content, true);
        }
    }
}