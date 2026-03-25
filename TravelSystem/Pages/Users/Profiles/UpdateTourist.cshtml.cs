using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using TravelSystem.Models;
using TravelSystem.Hubs;

namespace TravelSystem.Pages.Users.Profiles
{
    public class UpdateTouristModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<HubServer> _hubContext; // Dùng đúng HubServer của bạn

        public UpdateTouristModel(FinalPrnContext context, IWebHostEnvironment environment, IHubContext<HubServer> hubContext)
        {
            _context = context;
            _environment = environment;
            _hubContext = hubContext;
        }

        [BindProperty] public Tourist Tourist { get; set; } = new();
        [BindProperty] public IFormFile? FrontImageFile { get; set; }
        [BindProperty] public IFormFile? BackImageFile { get; set; }

        // Dùng Dictionary giống trang CreateAdmin của bạn
        public Dictionary<string, string> Errors { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            int? roleId = HttpContext.Session.GetInt32("UserRole");

            if (userId == null) return RedirectToPage("/Auths/Login");

            // CHẶN: Nếu đã là Role 5 thì không cho xác thực nữa
            if (roleId == 5) return RedirectToPage("/Index");

            var existingTourist = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == userId);
            if (existingTourist != null) Tourist = existingTourist;
            else Tourist.TouristId = userId.Value;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            ValidateTourist(); // Gọi hàm validate

            if (Errors.Count > 0) return Page();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var touristInDb = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == userId);
                bool isNew = (touristInDb == null);
                if (isNew) touristInDb = new Tourist { TouristId = userId.Value };

                touristInDb.AccountHolderName = Tourist.AccountHolderName?.ToUpper();
                touristInDb.IdCard = Tourist.IdCard;
                touristInDb.BankName = Tourist.BankName;
                touristInDb.BankNumber = Tourist.BankNumber;

                if (FrontImageFile != null) touristInDb.IdCardFrontImage = await SaveFile(FrontImageFile);
                if (BackImageFile != null) touristInDb.IdCardBackImage = await SaveFile(BackImageFile);

                if (isNew) _context.Tourists.Add(touristInDb);
                else _context.Tourists.Update(touristInDb);

                // Cập nhật Role sang 5
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.RoleId = 5;
                    _context.Users.Update(user);
                    HttpContext.Session.SetInt32("UserRole", 5); // Cập nhật Session ngay
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // SIGNALR BROADCAST
                await _hubContext.Clients.All.SendAsync("loadAll");

                TempData["SuccessPopup"] = "Xác thực thành công! Tài khoản đã được nâng cấp.";
                return Page();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Errors["System"] = "Lỗi: " + ex.Message;
                return Page();
            }
        }

        private void ValidateTourist()
        {
            if (string.IsNullOrWhiteSpace(Tourist.AccountHolderName))
                Errors["Name"] = "Tên chủ thẻ không được để trống!";

            if (string.IsNullOrWhiteSpace(Tourist.IdCard) || !System.Text.RegularExpressions.Regex.IsMatch(Tourist.IdCard, @"^\d{9,12}$"))
                Errors["IdCard"] = "Số CCCD phải từ 9-12 chữ số!";

            if (string.IsNullOrWhiteSpace(Tourist.BankNumber))
                Errors["BankNumber"] = "Số tài khoản không được để trống!";

            // Kiểm tra ảnh nếu là đăng ký mới
            var checkExist = _context.Tourists.AsNoTracking().Any(t => t.TouristId == Tourist.TouristId);
            if (!checkExist && (FrontImageFile == null || BackImageFile == null))
                Errors["Images"] = "Vui lòng tải lên ảnh CCCD mặt trước và mặt sau!";
        }

        private async Task<string> SaveFile(IFormFile file)
        {
            string folder = Path.Combine(_environment.WebRootPath, "images", "tourist");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string filePath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }
            return "/images/tourist/" + fileName;
        }
    }
}