using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.AgentProfiles
{
    public class EditProfileModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<HubServer> _hub;

        public EditProfileModel(Prn222PrjContext context, IWebHostEnvironment env, IHubContext<HubServer> hub)
        {
            _context = context; _env = env; _hub = hub;
        }

        public TravelAgent? Agent { get; set; }
        public User? CurrentUser { get; set; }

        [BindProperty] public string? TravelAgentName { get; set; }
        [BindProperty] public string? TravelAgentAddress { get; set; }
        [BindProperty] public string? TravelAgentGmail { get; set; }
        [BindProperty] public string? HotLine { get; set; }
        [BindProperty] public string? TaxCode { get; set; }
        [BindProperty] public string? EstablishmentDateRaw { get; set; }
        [BindProperty] public IFormFile? BusinessLicenseFile { get; set; }
        [BindProperty] public string? ExistingBusinessLicense { get; set; }

        [BindProperty] public string? RepresentativeIdcard { get; set; }
        [BindProperty] public string? DateOfIssueRaw { get; set; }
        [BindProperty] public IFormFile? FrontIdcardFile { get; set; }
        [BindProperty] public IFormFile? BackIdcardFile { get; set; }
        [BindProperty] public string? ExistingFrontIdcard { get; set; }
        [BindProperty] public string? ExistingBackIdcard { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            CurrentUser = await _context.Users.FindAsync(userId.Value);
            Agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            if (Agent == null) return NotFound();

            TravelAgentName = Agent.TravelAgentName;
            TravelAgentAddress = Agent.TravelAgentAddress;
            TravelAgentGmail = Agent.TravelAgentGmail;
            HotLine = Agent.HotLine;
            TaxCode = Agent.TaxCode;
            EstablishmentDateRaw = Agent.EstablishmentDate?.ToString("yyyy-MM-dd");
            ExistingBusinessLicense = Agent.BusinessLicense;
            RepresentativeIdcard = Agent.RepresentativeIdcard;
            DateOfIssueRaw = Agent.DateOfIssue?.ToString("yyyy-MM-dd");
            ExistingFrontIdcard = Agent.FrontIdcard;
            ExistingBackIdcard = Agent.BackIdcard;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            Agent = await _context.TravelAgents.FirstOrDefaultAsync(a => a.UserId == userId.Value);
            CurrentUser = await _context.Users.FindAsync(userId.Value);
            if (Agent == null) return NotFound();

            ModelState.Clear();
            ValidateInput();
            if (!ModelState.IsValid) return Page();

            if (BusinessLicenseFile != null)
                Agent.BusinessLicense = await SaveFile(BusinessLicenseFile, "images/registerAgent/");
            else if (!string.IsNullOrWhiteSpace(ExistingBusinessLicense))
                Agent.BusinessLicense = ExistingBusinessLicense;

            if (FrontIdcardFile != null)
                Agent.FrontIdcard = await SaveFile(FrontIdcardFile, "images/registerAgent/");
            else if (!string.IsNullOrWhiteSpace(ExistingFrontIdcard))
                Agent.FrontIdcard = ExistingFrontIdcard;

            if (BackIdcardFile != null)
                Agent.BackIdcard = await SaveFile(BackIdcardFile, "images/registerAgent");
            else if (!string.IsNullOrWhiteSpace(ExistingBackIdcard))
                Agent.BackIdcard = ExistingBackIdcard;

            Agent.TravelAgentName = TravelAgentName?.Trim();
            Agent.TravelAgentAddress = TravelAgentAddress?.Trim();
            Agent.TravelAgentGmail = TravelAgentGmail?.Trim();
            Agent.HotLine = HotLine?.Trim();
            Agent.TaxCode = TaxCode?.Trim();
            Agent.RepresentativeIdcard = RepresentativeIdcard?.Trim();

            if (DateOnly.TryParse(EstablishmentDateRaw, out var estDate))
                Agent.EstablishmentDate = estDate;
            if (DateOnly.TryParse(DateOfIssueRaw, out var issueDate))
                Agent.DateOfIssue = issueDate;

            await _context.SaveChangesAsync();

            _ = _hub.Clients.All.SendAsync("loadAll");

            TempData["EditProfileSuccess"] = "Cập nhật thông tin thành công!";
            return RedirectToPage("./Profile");
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            string dir = Path.Combine(_env.WebRootPath, folder);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return folder + "/" + fileName;
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TravelAgentName))
                ModelState.AddModelError("TravelAgentName", "Tên công ty không được để trống!");

            if (string.IsNullOrWhiteSpace(TravelAgentAddress))
                ModelState.AddModelError("TravelAgentAddress", "Địa chỉ không được để trống!");
            else if (TravelAgentAddress.Trim().Length < 5 || TravelAgentAddress.Trim().Length > 200)
                ModelState.AddModelError("TravelAgentAddress", "Địa chỉ phải từ 5 đến 200 ký tự!");

            if (string.IsNullOrWhiteSpace(TravelAgentGmail))
                ModelState.AddModelError("TravelAgentGmail", "Email không được để trống!");
            else if (!Regex.IsMatch(TravelAgentGmail.Trim(),
                     @"^[a-zA-Z0-9._%+-]+@(gmail\.com|[a-zA-Z0-9.-]+\.(com|net|org|vn|edu|edu\.vn|ac\.vn))$"))
                ModelState.AddModelError("TravelAgentGmail", "Email không hợp lệ!");

            if (string.IsNullOrWhiteSpace(HotLine))
                ModelState.AddModelError("HotLine", "Số điện thoại không được để trống!");
            else if (!Regex.IsMatch(HotLine.Trim(), @"^0\d{9}$"))
                ModelState.AddModelError("HotLine", "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số!");

            if (string.IsNullOrWhiteSpace(TaxCode))
                ModelState.AddModelError("TaxCode", "Mã số thuế không được để trống!");
            else if (!Regex.IsMatch(TaxCode.Trim(), @"^\d{10}$|^\d{13}$"))
                ModelState.AddModelError("TaxCode", "Mã số thuế phải có 10 hoặc 13 chữ số!");

            if (string.IsNullOrWhiteSpace(EstablishmentDateRaw))
                ModelState.AddModelError("EstablishmentDateRaw", "Ngày thành lập không được để trống!");
            else if (!DateOnly.TryParse(EstablishmentDateRaw, out var estDate))
                ModelState.AddModelError("EstablishmentDateRaw", "Ngày thành lập không hợp lệ!");
            else if (estDate >= DateOnly.FromDateTime(DateTime.Today))
                ModelState.AddModelError("EstablishmentDateRaw", "Ngày thành lập phải trước ngày hiện tại!");

            if (string.IsNullOrWhiteSpace(RepresentativeIdcard))
                ModelState.AddModelError("RepresentativeIdcard", "Số CCCD không được để trống!");
            else if (!Regex.IsMatch(RepresentativeIdcard.Trim(), @"^0\d{11}$"))
                ModelState.AddModelError("RepresentativeIdcard", "Số CCCD phải bắt đầu bằng 0 và có đúng 12 chữ số!");

            if (string.IsNullOrWhiteSpace(DateOfIssueRaw))
                ModelState.AddModelError("DateOfIssueRaw", "Ngày cấp CCCD không được để trống!");
            else if (!DateOnly.TryParse(DateOfIssueRaw, out var issueDate))
                ModelState.AddModelError("DateOfIssueRaw", "Ngày cấp không hợp lệ!");
            else if (issueDate >= DateOnly.FromDateTime(DateTime.Today))
                ModelState.AddModelError("DateOfIssueRaw", "Ngày cấp CCCD phải trước ngày hiện tại!");
            else if (CurrentUser?.Dob != null)
            {
                if (issueDate < CurrentUser.Dob.Value.AddYears(14))
                    ModelState.AddModelError("DateOfIssueRaw", "Ngày cấp CCCD phải sau ngày sinh ít nhất 14 năm!");
            }

            if (BusinessLicenseFile == null && string.IsNullOrWhiteSpace(ExistingBusinessLicense))
                ModelState.AddModelError("BusinessLicenseFile", "Vui lòng tải lên giấy phép kinh doanh!");
            if (FrontIdcardFile == null && string.IsNullOrWhiteSpace(ExistingFrontIdcard))
                ModelState.AddModelError("FrontIdcardFile", "Vui lòng tải lên CCCD mặt trước!");
            if (BackIdcardFile == null && string.IsNullOrWhiteSpace(ExistingBackIdcard))
                ModelState.AddModelError("BackIdcardFile", "Vui lòng tải lên CCCD mặt sau!");
        }
    }
}
