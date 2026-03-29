using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;
using TravelSystem.Services;

namespace TravelSystem.Pages.Staffs.Withdraws
{
    public class WithdrawRequestDetailModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IEmailService _emailService;

        public WithdrawRequestDetailModel(FinalPrnContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public FundRequest RequestItem { get; set; }
        public Tourist UserIdentity { get; set; } // Để xem ảnh CCCD đã xác thực
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 4) return RedirectToPage("/Auths/Login");

            RequestItem = await _context.FundRequests
                .Include(f => f.CreateByNavigation)
                .Include(f => f.ApproveByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (RequestItem == null) return NotFound();

            // Lấy thêm ảnh CCCD từ bảng Tourist để đối soát
            UserIdentity = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == RequestItem.CreateBy);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, string action, string reason)
        {
            var fundReq = await _context.FundRequests
                .Include(f => f.CreateByNavigation)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fundReq == null) return NotFound();

            int? staffId = HttpContext.Session.GetInt32("UserID");
            // Lấy giá trị rút an toàn (ép kiểu từ decimal? sang decimal)
            decimal withdrawAmount = fundReq.Amount ?? 0;

            if (action == "approve")
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Lấy ví (Hùng kiểm tra lại: ID 1 là Admin hay ID 6 như code cũ?)
                    var userWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == fundReq.CreateBy);
                    var systemWallet = await _context.Users.FirstOrDefaultAsync(w => w.UserId == 1);

                    if (userWallet == null || systemWallet == null)
                    {
                        ErrorMessage = "Lỗi: Không tìm thấy ví người dùng hoặc ví hệ thống.";
                        return Page();
                    }

                    if ((userWallet.Balance ?? 0) < withdrawAmount)
                    {
                        ErrorMessage = "Lỗi: Số dư ví khách hàng không đủ.";
                        return Page();
                    }

                    // 2. Trừ tiền (Sử dụng .Value hoặc ép kiểu decimal?)
                    userWallet.Balance = (userWallet.Balance ?? 0) - withdrawAmount;
                    systemWallet.Balance = (systemWallet.Balance ?? 0) - withdrawAmount;

                    // 3. Ghi lịch sử cho User
                    var userLog = new TransactionHistory
                    {
                        UserId = fundReq.CreateBy,
                        Amount = withdrawAmount,
                        TransactionType = "WITHDRAW",
                        Description = $"Rút tiền về {fundReq.BankNameSnapshot} ({fundReq.BankAccountSnapshot}) | [Số dư: {userWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    };
                    _context.TransactionHistories.Add(userLog);

                    // 4. Ghi lịch sử cho Hệ thống
                    var systemLog = new TransactionHistory
                    {
                        UserId = 1, // ID ví tổng Admin
                        Amount = withdrawAmount,
                        TransactionType = "PURCHASE",
                        Description = $"Chi trả rút tiền cho {fundReq.CreateByNavigation?.Gmail} | [Ví tổng: {systemWallet.Balance:N0}đ]",
                        TransactionDate = DateTime.Now
                    };
                    _context.TransactionHistories.Add(systemLog);

                    // 5. Cập nhật trạng thái Request
                    fundReq.Status = "APPROVED";
                    fundReq.ApproveBy = staffId;
                    fundReq.ApprovedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 6. Gửi mail
                    await _emailService.SendAsync(fundReq.CreateByNavigation?.Gmail, "Go Viet - Rút tiền thành công",
                        $"Yêu cầu rút {withdrawAmount:N0} VNĐ của bạn đã được duyệt.", true);

                    TempData["SuccessMessage"] = "Đã phê duyệt yêu cầu rút tiền thành công!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // In lỗi ra để Hùng debug nhanh
                    ErrorMessage = "Lỗi phát sinh: " + ex.Message;
                    return Page();
                }
            }
            else if (action == "reject")
            {
                // ... (Giữ nguyên logic Reject của Hùng)
                fundReq.Status = "REJECTED";
                fundReq.ApproveBy = staffId;
                fundReq.ApprovedDate = DateTime.Now;
                fundReq.ReferenceCode = "Lý do: " + reason;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã từ chối yêu cầu rút tiền.";
            }
            return RedirectToPage("./WithdrawManage");
        }
    }
}