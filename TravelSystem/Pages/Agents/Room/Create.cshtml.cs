using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Room
{
    public class CreateModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public CreateModel(FinalPrnContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TravelSystem.Models.Room Room { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int accommodationId { get; set; }

        public Accommodation? Accommodation { get; set; }

        public List<SelectListItem> RoomTypeOptions { get; set; } = new()
        {
            new SelectListItem { Value = "Phòng đơn", Text = "Phòng đơn" },
            new SelectListItem { Value = "Phòng đôi", Text = "Phòng đôi" },
            new SelectListItem { Value = "Phòng gia đình", Text = "Phòng gia đình" },
            new SelectListItem { Value = "Phòng tập thể", Text = "Phòng tập thể" },
            new SelectListItem { Value = "Phòng VIP", Text = "Phòng VIP" }
        };

        public IActionResult OnGet()
        {
            Accommodation = _context.Accommodations.FirstOrDefault(a => a.AccommodationId == accommodationId);

            if (Accommodation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách sạn.";
                return RedirectToPage("/Agents/Accommodations/Index");
            }

            Room.AccommodationId = accommodationId;
            return Page();
        }

        public IActionResult OnPost()
        {
            Accommodation = _context.Accommodations.FirstOrDefault(a => a.AccommodationId == accommodationId);

            if (Accommodation == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách sạn.";
                return RedirectToPage("/Agents/Accommodations/Index");
            }

            // nạp lại option khi post lỗi
            RoomTypeOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Phòng đơn", Text = "Phòng đơn" },
                new SelectListItem { Value = "Phòng đôi", Text = "Phòng đôi" },
                new SelectListItem { Value = "Phòng gia đình", Text = "Phòng gia đình" },
                new SelectListItem { Value = "Phòng tập thể", Text = "Phòng tập thể" },
                new SelectListItem { Value = "Phòng VIP", Text = "Phòng VIP" }
            };

            if (string.IsNullOrWhiteSpace(Room.RoomTypes))
            {
                ModelState.AddModelError("Room.RoomTypes", "Vui lòng chọn loại phòng.");
            }

            if (Room.NumberOfRooms == null || Room.NumberOfRooms <= 0)
            {
                ModelState.AddModelError("Room.NumberOfRooms", "Số lượng phòng phải lớn hơn 0.");
            }

            if (Room.PriceOfRoom == null || Room.PriceOfRoom <= 0)
            {
                ModelState.AddModelError("Room.PriceOfRoom", "Giá phòng phải lớn hơn 0.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Room.AccommodationId = accommodationId;
            _context.Rooms.Add(Room);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Tạo phòng thành công.";
            return RedirectToPage("/Agents/Room/Index");
        }
    }
}