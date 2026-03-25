using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.RegularExpressions;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.AgentRegisters
{
    public class AgentRegisterModel : PageModel
    {
        private readonly FinalPrnContext _context;
        private readonly IHubContext<HubServer> _hub;
        private readonly IWebHostEnvironment _env;

        public AgentRegisterModel(FinalPrnContext context, IHubContext<HubServer> hub, IWebHostEnvironment env)
        {
            _context = context;
            _hub = hub;
            _env = env;
        }

        public string UserFullName { get; set; } = "";
        public string UserGmail { get; set; } = "";
        public string UserPhone { get; set; } = "";

        public bool IsLoggedIn { get; private set; }
        public bool RegisterSuccess { get; private set; }

        public string TravelAgentName { get; set; } = "";
        public string TravelAgentGmail { get; set; } = "";
        public string HotLine { get; set; } = "";
        public string TravelAgentAddress { get; set; } = "";
        public string EstablishmentDate { get; set; } = "";
        public string TaxCode { get; set; } = "";
        public string RepresentativeIdcard { get; set; } = "";
        public string DateOfIssue { get; set; } = "";

        public IFormFile? BusinessLicenseFile { get; set; }
        public IFormFile? FrontIdcardFile { get; set; }
        public IFormFile? BackIdcardFile { get; set; }

        public string? TempBusinessLicensePath { get; set; }
        public string? TempFrontIdcardPath { get; set; }
        public string? TempBackIdcardPath { get; set; }

        public string? ErrAgentName { get; set; }
        public string? ErrAgentGmail { get; set; }
        public string? ErrHotLine { get; set; }
        public string? ErrAgentAddress { get; set; }
        public string? ErrEstDate { get; set; }
        public string? ErrTaxCode { get; set; }
        public string? ErrRepIdcard { get; set; }
        public string? ErrDateOfIssue { get; set; }
        public string? ErrBusinessLicense { get; set; }
        public string? ErrFrontIdcard { get; set; }
        public string? ErrBackIdcard { get; set; }

        [TempData] public string? GeneralError { get; set; }

        // ── Session keys ──
        private const string SessionFormKey = "agentFormData";
        private const string SessionErrKey = "agentFormErrors";
        private const string SessionSuccessKey = "agentRegisterSuccess";
        private const string SessionIsLoggedInKey = "agentRegisterIsLoggedIn";

        // ──────────────────────────────────────────────
        private JsonElement? GetPendingUser()
        {
            var json = HttpContext.Session.GetString("pendingUser");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<JsonElement>(json); }
            catch { return null; }
        }

        private void LoadUserInfoFromSession()
        {
            var p = GetPendingUser();
            if (p == null) return;
            var last = p.Value.TryGetProperty("LastName", out var l) ? l.GetString() ?? "" : "";
            var first = p.Value.TryGetProperty("FirstName", out var f) ? f.GetString() ?? "" : "";
            UserFullName = $"{last} {first}".Trim();
            UserGmail = p.Value.TryGetProperty("Gmail", out var g) ? g.GetString() ?? "" : "";
            UserPhone = p.Value.TryGetProperty("Phone", out var ph) ? ph.GetString() ?? "" : "";
        }

        private void LoadUserInfoFromDb(int userId)
        {
            var u = _context.Users.Find(userId);
            if (u == null) return;
            UserFullName = $"{u.LastName} {u.FirstName}".Trim();
            UserGmail = u.Gmail ?? "";
            UserPhone = u.Phone ?? "";
        }

        // ──────────────────────────────────────────────
        public IActionResult OnGet()
        {
            var successFlag = HttpContext.Session.GetString(SessionSuccessKey);
            if (successFlag == "1")
            {
                HttpContext.Session.Remove(SessionSuccessKey);
                IsLoggedIn = HttpContext.Session.GetString(SessionIsLoggedInKey) == "1";
                HttpContext.Session.Remove(SessionIsLoggedInKey);
                var uid = HttpContext.Session.GetInt32("UserID");
                if (uid != null) LoadUserInfoFromDb(uid.Value);
                else LoadUserInfoFromSession();

                RegisterSuccess = true;
                return Page();
            }

            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId != null)
            {
                IsLoggedIn = true;
                LoadUserInfoFromDb(userId.Value);
            }
            else if (GetPendingUser() != null)
            {
                IsLoggedIn = false;
                LoadUserInfoFromSession();
            }
            else
            {
                return RedirectToPage("/Users/Registers/Register");
            }

            RestoreFormFromSession();
            return Page();
        }

        // ──────────────────────────────────────────────
        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var pending = GetPendingUser();

            if (userId == null && pending == null)
                return RedirectToPage("/Users/Registers/Register");

            IsLoggedIn = userId != null;
            if (IsLoggedIn) LoadUserInfoFromDb(userId!.Value);
            else LoadUserInfoFromSession();

            TravelAgentName = Request.Form["TravelAgentName"].ToString().Trim();
            TravelAgentGmail = Request.Form["TravelAgentGmail"].ToString().Trim();
            HotLine = Request.Form["HotLine"].ToString().Trim();
            TravelAgentAddress = Request.Form["TravelAgentAddress"].ToString().Trim();
            EstablishmentDate = Request.Form["EstablishmentDate"].ToString().Trim();
            TaxCode = Request.Form["TaxCode"].ToString().Trim();
            RepresentativeIdcard = Request.Form["RepresentativeIdcard"].ToString().Trim();
            DateOfIssue = Request.Form["DateOfIssue"].ToString().Trim();

            BusinessLicenseFile = Request.Form.Files["BusinessLicenseFile"];
            FrontIdcardFile = Request.Form.Files["FrontIdcardFile"];
            BackIdcardFile = Request.Form.Files["BackIdcardFile"];

            var errors = ValidateInput();

            if (errors.Count > 0)
            {
                string? tmpBusiness = null, tmpFront = null, tmpBack = null;
                try
                {
                    var tempDir = Path.Combine(_env.WebRootPath, "images", "temp");
                    Directory.CreateDirectory(tempDir);

                    if (BusinessLicenseFile != null && BusinessLicenseFile.Length > 0 && IsValidImage(BusinessLicenseFile))
                        tmpBusiness = await SaveFileAsync(BusinessLicenseFile, tempDir, "/images/temp/");

                    if (FrontIdcardFile != null && FrontIdcardFile.Length > 0 && IsValidImage(FrontIdcardFile))
                        tmpFront = await SaveFileAsync(FrontIdcardFile, tempDir, "/images/temp/");

                    if (BackIdcardFile != null && BackIdcardFile.Length > 0 && IsValidImage(BackIdcardFile))
                        tmpBack = await SaveFileAsync(BackIdcardFile, tempDir, "/images/temp/");
                }
                catch { }

                SaveFormToSession(tmpBusiness, tmpFront, tmpBack);
                SaveErrorsToSession(errors);
                return RedirectToPage();
            }

            string? frontPath = null, backPath = null, businessPath = null;
            try
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "images", "registerAgent");
                var tempDir = Path.Combine(_env.WebRootPath, "images", "temp");
                Directory.CreateDirectory(uploadDir);
                Directory.CreateDirectory(tempDir);

                var tmpBusiness = GetTempPathFromSession("TempBusinessLicense");
                var tmpFront = GetTempPathFromSession("TempFrontIdcard");
                var tmpBack = GetTempPathFromSession("TempBackIdcard");

                if (BusinessLicenseFile != null && BusinessLicenseFile.Length > 0)
                    businessPath = await SaveFileAsync(BusinessLicenseFile, uploadDir, "images/registerAgent/");
                else if (!string.IsNullOrEmpty(tmpBusiness))
                    businessPath = MoveTempToFinal(tmpBusiness, tempDir, uploadDir);

                if (FrontIdcardFile != null && FrontIdcardFile.Length > 0)
                    frontPath = await SaveFileAsync(FrontIdcardFile, uploadDir, "images/registerAgent/");
                else if (!string.IsNullOrEmpty(tmpFront))
                    frontPath = MoveTempToFinal(tmpFront, tempDir, uploadDir);

                if (BackIdcardFile != null && BackIdcardFile.Length > 0)
                    backPath = await SaveFileAsync(BackIdcardFile, uploadDir, "images/registerAgent/");
                else if (!string.IsNullOrEmpty(tmpBack))
                    backPath = MoveTempToFinal(tmpBack, tempDir, uploadDir);
            }
            catch (Exception ex)
            {
                GeneralError = "Lỗi upload ảnh: " + ex.Message;
                return RedirectToPage();
            }

            try
            {
                int finalUserId;

                if (IsLoggedIn)
                {
                    finalUserId = userId!.Value;

                    var user = _context.Users.Find(finalUserId);
                    if (user != null)
                    {
                        user.RoleId = 3;
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var p = pending!.Value;
                    var user = new User
                    {
                        LastName = p.TryGetProperty("LastName", out var ln) ? ln.GetString() : "",
                        FirstName = p.TryGetProperty("FirstName", out var fn) ? fn.GetString() : "",
                        Phone = p.TryGetProperty("Phone", out var ph) ? ph.GetString() : "",
                        Gender = p.TryGetProperty("Gender", out var ge) ? ge.GetString() : "",
                        Gmail = p.TryGetProperty("Gmail", out var gm) ? gm.GetString() : "",
                        Address = p.TryGetProperty("Address", out var ad) ? ad.GetString() : "",
                        Password = p.TryGetProperty("Password", out var pw) ? pw.GetString() : "",
                        Dob = p.TryGetProperty("Dob", out var db)
                                        && DateOnly.TryParse(db.GetString(), out var dobVal)
                                        ? dobVal : (DateOnly?)null,
                        CreateAt = DateOnly.FromDateTime(DateTime.Now),
                        Status = 1,
                        RoleId = 3
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    finalUserId = user.UserId;
                }

                var agent = new TravelAgent
                {
                    UserId = finalUserId,
                    TravelAgentName = TravelAgentName,
                    TravelAgentAddress = TravelAgentAddress,
                    TravelAgentGmail = TravelAgentGmail,
                    HotLine = HotLine,
                    TaxCode = TaxCode,
                    EstablishmentDate = DateOnly.TryParse(EstablishmentDate, out var ed) ? ed : (DateOnly?)null,
                    RepresentativeIdcard = RepresentativeIdcard,
                    DateOfIssue = DateOnly.TryParse(DateOfIssue, out var doi) ? doi : (DateOnly?)null,
                    FrontIdcard = frontPath,
                    BackIdcard = backPath,
                    BusinessLicense = businessPath
                };

                _context.TravelAgents.Add(agent);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                GeneralError = "Đăng ký thất bại: " + ex.Message;
                return RedirectToPage();
            }

            try { await _hub.Clients.All.SendAsync("loadAll"); } catch { }

            // Dọn session form/error
            HttpContext.Session.Remove(SessionFormKey);
            HttpContext.Session.Remove(SessionErrKey);

            // Dọn pendingUser nếu là luồng đăng ký mới
            if (!IsLoggedIn)
            {
                HttpContext.Session.Remove("pendingUser");
                HttpContext.Session.Remove("gmail");
                HttpContext.Session.Remove("otp");
                HttpContext.Session.Remove("otpExpiry");
            }

            // Logout khỏi session hiện tại (cả 2 luồng đều cần đăng nhập lại sau khi đăng ký)
            var wasLoggedIn = IsLoggedIn;
            HttpContext.Session.Clear();

            // Lưu flag thành công vào Session sau khi clear
            HttpContext.Session.SetString(SessionSuccessKey, "1");
            HttpContext.Session.SetString(SessionIsLoggedInKey, wasLoggedIn ? "1" : "0");

            return RedirectToPage();
        }

        // ──────────────────────────────────────────────
        private Dictionary<string, string> ValidateInput()
        {
            var e = new Dictionary<string, string>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (string.IsNullOrWhiteSpace(TravelAgentName))
                e["AgentName"] = "Tên công ty không được để trống!";

            if (string.IsNullOrWhiteSpace(TravelAgentGmail))
                e["AgentGmail"] = "Email công ty không được để trống!";
            else if (!Regex.IsMatch(TravelAgentGmail,
                @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                e["AgentGmail"] = "Email công ty không hợp lệ!";
            else if (_context.TravelAgents.Any(a => a.TravelAgentGmail == TravelAgentGmail))
                e["AgentGmail"] = "Email công ty đã được sử dụng!";

            if (string.IsNullOrWhiteSpace(HotLine))
                e["HotLine"] = "Số HotLine không được để trống!";
            else if (!Regex.IsMatch(HotLine, @"^0\d{9}$"))
                e["HotLine"] = "Số HotLine phải bắt đầu bằng số 0 và có đúng 10 chữ số!";

            if (string.IsNullOrWhiteSpace(TravelAgentAddress))
                e["AgentAddress"] = "Địa chỉ công ty không được để trống!";
            else if (TravelAgentAddress.Length < 5 || TravelAgentAddress.Length > 200)
                e["AgentAddress"] = "Địa chỉ công ty phải từ 5 đến 200 ký tự!";

            if (string.IsNullOrWhiteSpace(TaxCode))
                e["TaxCode"] = "Mã số thuế không được để trống!";
            else if (!Regex.IsMatch(TaxCode, @"^\d{10}$|^\d{13}$"))
                e["TaxCode"] = "Mã số thuế phải có 10 hoặc 13 chữ số!";
            else if (_context.TravelAgents.Any(a => a.TaxCode == TaxCode))
                e["TaxCode"] = "Mã số thuế đã được sử dụng!";

            if (string.IsNullOrWhiteSpace(EstablishmentDate))
                e["EstDate"] = "Ngày thành lập không được để trống!";
            else if (!DateOnly.TryParse(EstablishmentDate, out var ed))
                e["EstDate"] = "Ngày thành lập không hợp lệ!";
            else if (ed >= today)
                e["EstDate"] = "Ngày thành lập phải trước ngày hiện tại!";

            if (string.IsNullOrWhiteSpace(RepresentativeIdcard))
                e["RepIdcard"] = "Số căn cước công dân không được để trống!";
            else if (!Regex.IsMatch(RepresentativeIdcard, @"^0\d{11}$"))
                e["RepIdcard"] = "Số căn cước công dân phải bắt đầu bằng số 0 và có đúng 12 chữ số!";
            else if (_context.TravelAgents.Any(a => a.RepresentativeIdcard == RepresentativeIdcard))
                e["RepIdcard"] = "Số căn cước công dân đã được sử dụng!";

            DateOnly? dobVal = null;
            var loggedInId = HttpContext.Session.GetInt32("UserID");
            if (loggedInId != null)
            {
                dobVal = _context.Users.Find(loggedInId.Value)?.Dob;
            }
            else
            {
                var pendingJson = HttpContext.Session.GetString("pendingUser");
                if (!string.IsNullOrEmpty(pendingJson))
                {
                    try
                    {
                        var p = JsonSerializer.Deserialize<JsonElement>(pendingJson);
                        if (p.TryGetProperty("Dob", out var db) && DateOnly.TryParse(db.GetString(), out var d))
                            dobVal = d;
                    }
                    catch { }
                }
            }

            if (string.IsNullOrWhiteSpace(DateOfIssue))
                e["DateOfIssue"] = "Ngày cấp không được để trống!";
            else if (!DateOnly.TryParse(DateOfIssue, out var doi))
                e["DateOfIssue"] = "Ngày cấp không hợp lệ!";
            else if (doi >= today)
                e["DateOfIssue"] = "Ngày cấp phải trước ngày hiện tại!";
            else if (dobVal.HasValue && doi < dobVal.Value.AddYears(14))
                e["DateOfIssue"] = "Ngày cấp phải sau ngày sinh ít nhất 14 năm!";

            var tempBusiness = GetTempPathFromSession("TempBusinessLicense");
            if (BusinessLicenseFile == null || BusinessLicenseFile.Length == 0)
            {
                if (string.IsNullOrEmpty(tempBusiness))
                    e["BusinessLicense"] = "Vui lòng tải lên giấy phép kinh doanh!";
            }
            else if (!IsValidImage(BusinessLicenseFile))
                e["BusinessLicense"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png)!";

            var tempFront = GetTempPathFromSession("TempFrontIdcard");
            if (FrontIdcardFile == null || FrontIdcardFile.Length == 0)
            {
                if (string.IsNullOrEmpty(tempFront))
                    e["FrontIdcard"] = "Vui lòng tải lên CCCD mặt trước!";
            }
            else if (!IsValidImage(FrontIdcardFile))
                e["FrontIdcard"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png)!";

            var tempBack = GetTempPathFromSession("TempBackIdcard");
            if (BackIdcardFile == null || BackIdcardFile.Length == 0)
            {
                if (string.IsNullOrEmpty(tempBack))
                    e["BackIdcard"] = "Vui lòng tải lên CCCD mặt sau!";
            }
            else if (!IsValidImage(BackIdcardFile))
                e["BackIdcard"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png)!";

            return e;
        }

        private string? GetTempPathFromSession(string field)
        {
            var formJson = HttpContext.Session.GetString(SessionFormKey);
            if (string.IsNullOrEmpty(formJson)) return null;
            try
            {
                var d = JsonSerializer.Deserialize<AgentFormData>(formJson);
                return field switch
                {
                    "TempBusinessLicense" => d?.TempBusinessLicense,
                    "TempFrontIdcard" => d?.TempFrontIdcard,
                    "TempBackIdcard" => d?.TempBackIdcard,
                    _ => null
                };
            }
            catch { return null; }
        }

        private bool IsValidImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            return allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
        }

        private async Task<string> SaveFileAsync(IFormFile file, string uploadDir, string urlPrefix)
        {
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"{urlPrefix}{fileName}";
        }

        private async Task<string> SaveFileAsync(IFormFile file, string uploadDir)
            => await SaveFileAsync(file, uploadDir, "images/registerAgent/");

        private string MoveTempToFinal(string tempUrlPath, string tempDir, string uploadDir)
        {
            var fileName = Path.GetFileName(tempUrlPath);
            var srcPath = Path.Combine(tempDir, fileName);
            var ext = Path.GetExtension(fileName);
            var newFileName = $"{Guid.NewGuid()}{ext}";
            var destPath = Path.Combine(uploadDir, newFileName);

            if (System.IO.File.Exists(srcPath))
                System.IO.File.Move(srcPath, destPath);
            else
                throw new FileNotFoundException($"Ảnh tạm không tìm thấy: {srcPath}");

            return $"images/registerAgent/{newFileName}";
        }

        private void CleanupTempImages()
        {
            try
            {
                var formJson = HttpContext.Session.GetString(SessionFormKey);
                if (string.IsNullOrEmpty(formJson)) return;
                var d = JsonSerializer.Deserialize<AgentFormData>(formJson);
                if (d == null) return;
                var tempPaths = new[] { d.TempBusinessLicense, d.TempFrontIdcard, d.TempBackIdcard };
                foreach (var urlPath in tempPaths)
                {
                    if (string.IsNullOrEmpty(urlPath)) continue;
                    var fileName = Path.GetFileName(urlPath);
                    var fullPath = Path.Combine(_env.WebRootPath, "images", "temp", fileName);
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
            }
            catch { }
        }

        private void SaveFormToSession(string? tmpBusiness = null, string? tmpFront = null, string? tmpBack = null)
        {
            var oldBusiness = tmpBusiness ?? GetTempPathFromSession("TempBusinessLicense");
            var oldFront = tmpFront ?? GetTempPathFromSession("TempFrontIdcard");
            var oldBack = tmpBack ?? GetTempPathFromSession("TempBackIdcard");

            var data = new AgentFormData
            {
                AgentName = TravelAgentName,
                AgentGmail = TravelAgentGmail,
                HotLine = HotLine,
                AgentAddress = TravelAgentAddress,
                EstablishmentDate = EstablishmentDate,
                TaxCode = TaxCode,
                RepIdcard = RepresentativeIdcard,
                DateOfIssue = DateOfIssue,
                TempBusinessLicense = tmpBusiness ?? oldBusiness,
                TempFrontIdcard = tmpFront ?? oldFront,
                TempBackIdcard = tmpBack ?? oldBack
            };
            HttpContext.Session.SetString(SessionFormKey, JsonSerializer.Serialize(data));
        }

        private void SaveErrorsToSession(Dictionary<string, string> errors)
        {
            var errs = new AgentFormErrors
            {
                AgentName = errors.GetValueOrDefault("AgentName"),
                AgentGmail = errors.GetValueOrDefault("AgentGmail"),
                HotLine = errors.GetValueOrDefault("HotLine"),
                AgentAddress = errors.GetValueOrDefault("AgentAddress"),
                EstDate = errors.GetValueOrDefault("EstDate"),
                TaxCode = errors.GetValueOrDefault("TaxCode"),
                RepIdcard = errors.GetValueOrDefault("RepIdcard"),
                DateOfIssue = errors.GetValueOrDefault("DateOfIssue"),
                BusinessLicense = errors.GetValueOrDefault("BusinessLicense"),
                FrontIdcard = errors.GetValueOrDefault("FrontIdcard"),
                BackIdcard = errors.GetValueOrDefault("BackIdcard")
            };
            HttpContext.Session.SetString(SessionErrKey, JsonSerializer.Serialize(errs));
        }

        private void RestoreFormFromSession()
        {
            var formJson = HttpContext.Session.GetString(SessionFormKey);
            var errJson = HttpContext.Session.GetString(SessionErrKey);

            if (!string.IsNullOrEmpty(formJson))
            {
                try
                {
                    var d = JsonSerializer.Deserialize<AgentFormData>(formJson);
                    if (d != null)
                    {
                        TravelAgentName = d.AgentName ?? "";
                        TravelAgentGmail = d.AgentGmail ?? "";
                        HotLine = d.HotLine ?? "";
                        TravelAgentAddress = d.AgentAddress ?? "";
                        EstablishmentDate = d.EstablishmentDate ?? "";
                        TaxCode = d.TaxCode ?? "";
                        RepresentativeIdcard = d.RepIdcard ?? "";
                        DateOfIssue = d.DateOfIssue ?? "";
                        TempBusinessLicensePath = d.TempBusinessLicense;
                        TempFrontIdcardPath = d.TempFrontIdcard;
                        TempBackIdcardPath = d.TempBackIdcard;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(errJson))
            {
                try
                {
                    var er = JsonSerializer.Deserialize<AgentFormErrors>(errJson);
                    if (er != null)
                    {
                        ErrAgentName = er.AgentName;
                        ErrAgentGmail = er.AgentGmail;
                        ErrHotLine = er.HotLine;
                        ErrAgentAddress = er.AgentAddress;
                        ErrEstDate = er.EstDate;
                        ErrTaxCode = er.TaxCode;
                        ErrRepIdcard = er.RepIdcard;
                        ErrDateOfIssue = er.DateOfIssue;
                        ErrBusinessLicense = er.BusinessLicense;
                        ErrFrontIdcard = er.FrontIdcard;
                        ErrBackIdcard = er.BackIdcard;
                    }
                }
                catch { }
                HttpContext.Session.Remove(SessionErrKey);
            }
        }
    }

    public class AgentFormData
    {
        public string? AgentName { get; set; }
        public string? AgentGmail { get; set; }
        public string? HotLine { get; set; }
        public string? AgentAddress { get; set; }
        public string? EstablishmentDate { get; set; }
        public string? TaxCode { get; set; }
        public string? RepIdcard { get; set; }
        public string? DateOfIssue { get; set; }
        public string? TempBusinessLicense { get; set; }
        public string? TempFrontIdcard { get; set; }
        public string? TempBackIdcard { get; set; }
    }

    public class AgentFormErrors
    {
        public string? AgentName { get; set; }
        public string? AgentGmail { get; set; }
        public string? HotLine { get; set; }
        public string? AgentAddress { get; set; }
        public string? EstDate { get; set; }
        public string? TaxCode { get; set; }
        public string? RepIdcard { get; set; }
        public string? DateOfIssue { get; set; }
        public string? BusinessLicense { get; set; }
        public string? FrontIdcard { get; set; }
        public string? BackIdcard { get; set; }
    }
}
