using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IHubContext<HubServer> _hubContext;

        public UpdateTouristModel(FinalPrnContext context, IWebHostEnvironment environment, IHubContext<HubServer> hubContext)
        {
            _context = context;
            _environment = environment;
            _hubContext = hubContext;
        }

        [BindProperty] public Tourist Tourist { get; set; } = new();
        [BindProperty] public IFormFile? FrontImageFile { get; set; }
        [BindProperty] public IFormFile? BackImageFile { get; set; }

        // Đường dẫn ảnh tạm (giống AgentRegister)
        public string? TempFrontImagePath { get; set; }
        public string? TempBackImagePath { get; set; }

        public Dictionary<string, string> Errors { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            int? roleId = HttpContext.Session.GetInt32("UserRole");

            if (userId == null) return RedirectToPage("/Auths/Login");
            if (roleId == 5) return RedirectToPage("/Index");

            var existingTourist = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == userId);
            if (existingTourist != null) Tourist = existingTourist;
            else Tourist.TouristId = userId.Value;

            // Restore ảnh tạm từ TempData (sau redirect do lỗi)
            TempFrontImagePath = TempData["TempFrontImagePath"] as string;
            TempBackImagePath = TempData["TempBackImagePath"] as string;

            // Restore lỗi từ TempData
            if (TempData["Errors"] is string errJson)
            {
                try
                {
                    Errors = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string>>(errJson) ?? new();
                }
                catch { }
            }

            // Restore dữ liệu form đã nhập từ TempData
            if (TempData["AccountHolderName"] is string name) Tourist.AccountHolderName = name;
            if (TempData["IdCard"] is string idCard) Tourist.IdCard = idCard;
            if (TempData["BankName"] is string bankName) Tourist.BankName = bankName;
            if (TempData["BankNumber"] is string bankNumber) Tourist.BankNumber = bankNumber;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            var touristInDb = await _context.Tourists.FirstOrDefaultAsync(t => t.TouristId == userId);

            ValidateTourist(touristInDb != null);

            if (Errors.Count > 0)
            {

                string? tempFront = null;
                string? tempBack = null;

                try
                {
                    var tempDir = Path.Combine(_environment.WebRootPath, "images", "temp");
                    Directory.CreateDirectory(tempDir);

                    if (FrontImageFile != null && FrontImageFile.Length > 0 && IsValidImage(FrontImageFile))
                        tempFront = await SaveTempFile(FrontImageFile, tempDir);

                    if (BackImageFile != null && BackImageFile.Length > 0 && IsValidImage(BackImageFile))
                        tempBack = await SaveTempFile(BackImageFile, tempDir);
                }
                catch { }

                // Giữ lại đường dẫn ảnh tạm cũ nếu lần này không upload ảnh mới
                if (string.IsNullOrEmpty(tempFront))
                    tempFront = Request.Form["TempFrontImagePath"];
                if (string.IsNullOrEmpty(tempBack))
                    tempBack = Request.Form["TempBackImagePath"];

                // Truyền toàn bộ state sang GET qua TempData
                TempData["TempFrontImagePath"] = tempFront;
                TempData["TempBackImagePath"] = tempBack;
                TempData["Errors"] = System.Text.Json.JsonSerializer.Serialize(Errors);
                TempData["AccountHolderName"] = Tourist.AccountHolderName;
                TempData["IdCard"] = Tourist.IdCard;
                TempData["BankName"] = Tourist.BankName;
                TempData["BankNumber"] = Tourist.BankNumber;

                return RedirectToPage();
            }

            // Validation pass → lưu chính thức
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool isNew = (touristInDb == null);
                if (isNew) touristInDb = new Tourist { TouristId = userId.Value };

                touristInDb.AccountHolderName = Tourist.AccountHolderName?.ToUpper();
                touristInDb.IdCard = Tourist.IdCard;
                touristInDb.BankName = Tourist.BankName;
                touristInDb.BankNumber = Tourist.BankNumber;

                var uploadDir = Path.Combine(_environment.WebRootPath, "images", "tourist");
                var tempDir = Path.Combine(_environment.WebRootPath, "images", "temp");
                Directory.CreateDirectory(uploadDir);

                // Ảnh mặt trước: ưu tiên file mới → ảnh tạm → giữ nguyên ảnh cũ
                if (FrontImageFile != null && FrontImageFile.Length > 0)
                    touristInDb.IdCardFrontImage = await SaveFinalFile(FrontImageFile, uploadDir);
                else
                {
                    var tmpFront = Request.Form["TempFrontImagePath"].ToString();
                    if (!string.IsNullOrEmpty(tmpFront))
                        touristInDb.IdCardFrontImage = MoveTempToFinal(tmpFront, tempDir, uploadDir);
                }

                // Ảnh mặt sau: ưu tiên file mới → ảnh tạm → giữ nguyên ảnh cũ
                if (BackImageFile != null && BackImageFile.Length > 0)
                    touristInDb.IdCardBackImage = await SaveFinalFile(BackImageFile, uploadDir);
                else
                {
                    var tmpBack = Request.Form["TempBackImagePath"].ToString();
                    if (!string.IsNullOrEmpty(tmpBack))
                        touristInDb.IdCardBackImage = MoveTempToFinal(tmpBack, tempDir, uploadDir);
                }

                if (isNew) _context.Tourists.Add(touristInDb);
                else _context.Tourists.Update(touristInDb);

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.RoleId = 5;
                    _context.Users.Update(user);
                    HttpContext.Session.SetInt32("RoleId", 5);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients.All.SendAsync("loadAll");

                TempData["SuccessPopup"] = "Xác thực thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Errors["System"] = "Lỗi hệ thống: " + ex.Message;

                TempData["Errors"] = System.Text.Json.JsonSerializer.Serialize(Errors);
                TempData["AccountHolderName"] = Tourist.AccountHolderName;
                TempData["IdCard"] = Tourist.IdCard;
                TempData["BankName"] = Tourist.BankName;
                TempData["BankNumber"] = Tourist.BankNumber;

                return RedirectToPage();
            }
        }

        private void ValidateTourist(bool hasExistingRecord)
        {
            if (string.IsNullOrWhiteSpace(Tourist.AccountHolderName))
                Errors["Name"] = "Tên chủ thẻ không được để trống!";

            if (string.IsNullOrWhiteSpace(Tourist.IdCard) ||
                !System.Text.RegularExpressions.Regex.IsMatch(Tourist.IdCard, @"^0\d{11}$"))
                Errors["IdCard"] = "Số CCCD phải đúng 12 chữ số và bắt đầu bằng 0!";

            if (string.IsNullOrWhiteSpace(Tourist.BankNumber))
                Errors["BankNumber"] = "Số tài khoản không được để trống!";

            // Chỉ bắt buộc ảnh nếu là tạo mới VÀ không có ảnh tạm
            if (!hasExistingRecord)
            {
                var hasFront = FrontImageFile != null ||
                               !string.IsNullOrEmpty(Request.Form["TempFrontImagePath"]);
                var hasBack = BackImageFile != null ||
                               !string.IsNullOrEmpty(Request.Form["TempBackImagePath"]);

                if (!hasFront) Errors["FrontImage"] = "Vui lòng chọn ảnh CCCD mặt trước!";
                if (!hasBack) Errors["BackImage"] = "Vui lòng chọn ảnh CCCD mặt sau!";
            }
        }

        private bool IsValidImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            return allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
        }

        private async Task<string> SaveTempFile(IFormFile file, string tempDir)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(tempDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return "/images/temp/" + fileName;
        }

        private async Task<string> SaveFinalFile(IFormFile file, string uploadDir)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return "images/tourist/" + fileName;
        }

        private string MoveTempToFinal(string tempUrlPath, string tempDir, string uploadDir)
        {
            var fileName = Path.GetFileName(tempUrlPath);
            var srcPath = Path.Combine(tempDir, fileName);
            var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var destPath = Path.Combine(uploadDir, newFileName);

            if (System.IO.File.Exists(srcPath))
                System.IO.File.Move(srcPath, destPath);

            return "images/tourist/" + newFileName;
        }
    }
}
