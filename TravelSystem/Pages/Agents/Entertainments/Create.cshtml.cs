using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace TravelSystem.Pages.Agents.Entertainments
{
    public class CreateModel : PageModel
    {
        private readonly Prn222PrjContext _context;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(Prn222PrjContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadImage { get; set; }

        public List<SelectListItem> EntertainmentTypes { get; set; } = new();
        public List<SelectListItem> Cities { get; set; } = new();
        public List<SelectListItem> StatusList { get; set; } = new();

        public void OnGet()
        {
            LoadData();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadData();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            int travelAgentId = GetCurrentTravelAgentId();

            string? imagePath = null;

            if (UploadImage != null && UploadImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadImage.FileName);
                var folderPath = Path.Combine(_environment.WebRootPath, "images", "entertainments");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadImage.CopyToAsync(stream);
                }

                imagePath = Path.Combine("images", "entertainments", fileName).Replace("\\", "/");
            }

            var service = new Service
            {
                ServiceName = Input.Name?.Trim(),
                ServiceType = "ENTERTAINMENT",
                TravelAgentId = travelAgentId
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            var entertainment = new Entertainment
            {
                ServiceId = service.ServiceId,
                Name = Input.Name?.Trim(),
                Image = imagePath,
                Address = Input.City?.Trim(),
                Phone = Input.Phone?.Trim(),
                Description = Input.Description?.Trim(),
                TimeOpen = Input.TimeOpen,
                TimeClose = Input.TimeClose,
                DayOfWeekOpen = Input.DayOfWeekOpen != null
                    ? string.Join(", ", Input.DayOfWeekOpen)
                    : null,
                TicketPrice = Input.TicketPrice,
                Rate = Input.Rate,
                Status = Input.Status,
                EntertainmentType = Input.Type
            };

            _context.Entertainments.Add(entertainment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "ĐÃ THÊM DỊCH VỤ THÀNH CÔNG";
            return RedirectToPage("./Index");
        }

        private void LoadData()
        {
            EntertainmentTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Khu vui chơi", Text = "Khu vui chơi" },
                new SelectListItem { Value = "Rạp chiếu phim", Text = "Rạp chiếu phim" },
                new SelectListItem { Value = "Bảo tàng", Text = "Bảo tàng" },
                new SelectListItem { Value = "Công viên", Text = "Công viên" },
                new SelectListItem { Value = "Khác", Text = "Khác" }
            };

            Cities = new List<SelectListItem>
            {
                new SelectListItem { Value = "Hà Nội", Text = "Hà Nội" },
                new SelectListItem { Value = "Hồ Chí Minh", Text = "Hồ Chí Minh" },
                new SelectListItem { Value = "Đà Nẵng", Text = "Đà Nẵng" },
                new SelectListItem { Value = "Huế", Text = "Huế" },
                new SelectListItem { Value = "Nha Trang", Text = "Nha Trang" }
            };

            StatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Hoạt động", Text = "Hoạt động" },
                new SelectListItem { Value = "Ngưng hoạt động", Text = "Ngưng hoạt động" }
            };
        }

        private int GetCurrentTravelAgentId()
        {
            return 1;
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
            public string? Name { get; set; }

            [Required(ErrorMessage = "Loại giải trí không được để trống")]
            public string? Type { get; set; }

            [Required(ErrorMessage = "Tỉnh / thành phố không được để trống")]
            public string? City { get; set; }

            [Required(ErrorMessage = "Số điện thoại không được để trống")]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số")]
            public string? Phone { get; set; }

            [Required(ErrorMessage = "Đánh giá không được để trống")]
            [Range(0, 10, ErrorMessage = "Đánh giá phải từ 0 đến 10")]
            public double? Rate { get; set; }

            [Required(ErrorMessage = "Thời gian mở cửa không được để trống")]
            public TimeOnly? TimeOpen { get; set; }

            [Required(ErrorMessage = "Thời gian đóng cửa không được để trống")]
            public TimeOnly? TimeClose { get; set; }

            [Required(ErrorMessage = "Ngày mở cửa trong tuần không được để trống")]
            public List<string>? DayOfWeekOpen { get; set; }

            [Required(ErrorMessage = "Giá vé không được để trống")]
            [Range(0, double.MaxValue, ErrorMessage = "Giá vé phải lớn hơn hoặc bằng 0")]
            public double? TicketPrice { get; set; }

            [Required(ErrorMessage = "Mô tả dịch vụ không được để trống")]
            public string? Description { get; set; }

            [Required(ErrorMessage = "Trạng thái không được để trống")]
            public string? Status { get; set; }
        }
    }
}