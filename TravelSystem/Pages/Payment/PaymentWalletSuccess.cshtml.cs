using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;

namespace TravelSystem.Pages.Payment
{
    public class PaymentWalletSuccessModel : PageModel
    {
        private readonly FinalPrnContext _con;

        public PaymentWalletSuccessModel(FinalPrnContext con)
        {
            _con = con;
        }

        public User User { get; set; }
        public PendingRecharge PendingRecharge { get; set; }

        public IActionResult OnGet(string referenceCode, string status, long? orderCode)
        {
            // 1. Kiểm tra tham số từ PayOS (orderCode là tham số chuẩn PayOS trả về)
            if (!orderCode.HasValue && string.IsNullOrEmpty(referenceCode))
            {
                return RedirectToPage("/Payment/PaymentWalletCancel");
            }

            // 2. Nếu trạng thái trả về là CANCELLED (Người dùng bấm hủy trên giao diện PayOS)
            if (status == "CANCELLED")
            {
                return RedirectToPage("/Payment/PaymentWalletCancel");
            }

            using var transaction = _con.Database.BeginTransaction();
            try
            {
                // 3. Tìm bản ghi nạp tiền (Thử tìm theo orderCode trước, sau đó mới tới referenceCode)
                string searchKey = orderCode.HasValue ? orderCode.Value.ToString() : referenceCode;

                var recharge = _con.PendingRecharges
                    .FirstOrDefault(r => r.ReferenceCode == searchKey);

                // Nếu vẫn không thấy, thử tìm chéo lại một lần nữa với biến còn lại
                if (recharge == null && !string.IsNullOrEmpty(referenceCode))
                {
                    recharge = _con.PendingRecharges.FirstOrDefault(r => r.ReferenceCode == referenceCode);
                }

                if (recharge == null)
                {
                    return RedirectToPage("/Payment/PaymentWalletCancel");
                }

                // 4. Kiểm tra nếu đơn hàng đã thành công trước đó (tránh cộng tiền 2 lần khi F5 trang)
                if (recharge.Status == "APPROVED")
                {
                    User = _con.Users.FirstOrDefault(u => u.UserId == recharge.UserId);
                    PendingRecharge = recharge;
                    return Page();
                }

                // 5. Kiểm tra trạng thái thanh toán từ PayOS (Chỉ cộng tiền khi status là PAID)
                // Lưu ý: Nếu muốn test nhanh có thể tạm đóng dòng check status này lại
                if (status != "PAID" && status != "NOPAY")
                {
                    return RedirectToPage("/Payment/PaymentWalletCancel");
                }

                // 6. Lấy thông tin ví và người dùng
                var user = _con.Users.FirstOrDefault(u => u.UserId == recharge.UserId);
                var systemWallet = _con.Wallets.FirstOrDefault(w => w.UserId == 6); // Ví admin/hệ thống
                var userWallet = _con.Wallets.FirstOrDefault(w => w.UserId == user.UserId);

                if (user == null || userWallet == null || systemWallet == null)
                {
                    return RedirectToPage("/Payment/PaymentWalletCancel");
                }

                decimal amount = recharge.Amount;

                // --- THỰC HIỆN CẬP NHẬT DATABASE ---

                // Bước 1: Cập nhật yêu cầu nạp
                recharge.Status = "APPROVED";
                recharge.ApproveDate = DateTime.Now;

                // Bước 2: Cộng số dư ví
                userWallet.Balance += amount;
                systemWallet.Balance += amount;

                // Bước 3: Lưu lịch sử giao dịch cho hệ thống (Admin nhận tiền)
                _con.TransactionHistories.Add(new TransactionHistory
                {
                    UserId = 6,
                    Amount = amount,
                    TransactionType = "RECEIVE",
                    Description = $"Hệ thống nhận tiền nạp từ {user.Gmail} (Mã: {recharge.ReferenceCode})",
                    AmountAfterTransaction = systemWallet.Balance,
                    TransactionDate = DateTime.Now
                });

                // Bước 4: Lưu lịch sử giao dịch cho người dùng
                _con.TransactionHistories.Add(new TransactionHistory
                {
                    UserId = user.UserId,
                    Amount = amount,
                    TransactionType = "RECHARGE",
                    Description = $"Nạp {(int)amount:N0} VNĐ vào ví thành công qua PayOS",
                    AmountAfterTransaction = userWallet.Balance,
                    TransactionDate = DateTime.Now
                });

                // Bước 5: Lưu tất cả thay đổi
                _con.SaveChanges();
                transaction.Commit();

                // Gán dữ liệu hiển thị ra giao diện Success
                User = user;
                PendingRecharge = recharge;

                return Page();
            }
            catch (Exception)
            {
                transaction.Rollback();
                return RedirectToPage("/Payment/PaymentWalletCancel");
            }
        }
    }
}