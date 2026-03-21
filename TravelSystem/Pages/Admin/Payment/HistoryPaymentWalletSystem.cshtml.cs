using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TravelSystem.Pages.Admin.Payment;
public class HistoryPaymentWalletSystemModel : PageModel
{
    private readonly Prn222PrjContext _context;
    public HistoryPaymentWalletSystemModel(Prn222PrjContext context) => _context = context;

    public List<TransactionHistory> TransactionList { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    // Các thuộc tính phục vụ phân trang
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int StartIndex { get; set; }

    public async Task<IActionResult> OnGetAsync(int page = 1)
    {
        var userIdSession = HttpContext.Session.GetInt32("UserID");
        if (userIdSession == null) return RedirectToPage("/Auths/Login");

        const int pageSize = 5; 
        CurrentPage = page;

        var query = _context.TransactionHistories
            .Where(t => t.UserId == 6) 
            .AsQueryable();

        if (!string.IsNullOrEmpty(Status) && Status != "0")
            query = query.Where(t => t.TransactionType == Status);

        if (FromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= FromDate.Value);

        if (ToDate.HasValue)
        {
            var endOfDay = ToDate.Value.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.TransactionDate <= endOfDay);
        }

        // --- Logic Phân Trang ---
        int totalItems = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Đảm bảo số trang hợp lệ
        if (CurrentPage < 1) CurrentPage = 1;
        if (TotalPages > 0 && CurrentPage > TotalPages) CurrentPage = TotalPages;

        StartIndex = (CurrentPage - 1) * pageSize + 1;

        TransactionList = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Page();
    }
}