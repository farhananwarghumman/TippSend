using TippSendApp.Data;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Admin.Orders;

[Authorize(Policy = "RequireAdmin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly OrderService _orderService;

    public IndexModel(ApplicationDbContext db, OrderService orderService)
    {
        _db = db;
        _orderService = orderService;
    }

    public List<Order> Orders { get; private set; } = new();
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Date { get; set; }

    public async Task OnGetAsync()
    {
        var query = _db.Orders.Include(o => o.Shop).AsQueryable();

        if (Enum.TryParse<OrderStatus>(Status, out var statusFilter))
            query = query.Where(o => o.Status == statusFilter);

        if (Date.HasValue)
            query = query.Where(o => o.DeliveryDate.Date == Date.Value.Date);

        if (!string.IsNullOrWhiteSpace(Search))
            query = query.Where(o => o.Reference.Contains(Search)
                || o.RecipientName.Contains(Search)
                || o.SenderName.Contains(Search)
                || o.ItemDescription.Contains(Search));

        // SQLite doesn't support TimeSpan in ORDER BY — materialise first, then sort in memory
        Orders = (await query.ToListAsync())
            .OrderByDescending(o => o.DeliveryDate)
            .ThenBy(o => o.DeliveryWindowStart)
            .ToList();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, OrderStatus newStatus)
    {
        await _orderService.UpdateStatusAsync(id, newStatus);
        return RedirectToPage(new { Status, Search, Date });
    }
}
