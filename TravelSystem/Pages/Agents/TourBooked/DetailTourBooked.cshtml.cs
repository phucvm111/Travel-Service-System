using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelSystem.Models;

namespace TravelSystem.Pages.Agents.Tours
{
    public class DetailTourBookedModel : PageModel
    {
        private readonly FinalPrnContext _context;

        public DetailTourBookedModel(FinalPrnContext context)
        {
            _context = context;
        }

        public BookDetail BookDetail { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToPage("/Auths/Login");

            // Truy vấn đơn hàng kèm đầy đủ thông tin Tour và Voucher liên quan
            BookDetail = await _context.BookDetails
                .Include(b => b.Tour)
                .Include(b => b.Voucher)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (BookDetail == null)
            {
                ErrorMessage = "Không tìm thấy chi tiết đặt tour với ID: " + id;
                return Page();
            }

            return Page();
        }
    }
}