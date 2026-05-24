using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    public int OrdersToday { get; private set; }
    public int DeliveredToday { get; private set; }
    public int PendingToday { get; private set; }
    public int PendingAllTime { get; private set; }
    public decimal RevenueThisWeek { get; private set; }
    public decimal RevenueThisMonth { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;

        var todayOrders = await _db.Orders
            .Where(o => o.DeliveryDate.Date == today)
            .Select(o => new { o.Status, o.Total })
            .ToListAsync();

        OrdersToday    = todayOrders.Count;
        DeliveredToday = todayOrders.Count(o => o.Status == OrderStatus.Delivered);
        PendingToday   = todayOrders.Count(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Failed);

        PendingAllTime = await _db.Orders
            .CountAsync(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Failed);

        var weekStart  = IsoWeekStart(today);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        RevenueThisWeek = await _db.Orders
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= weekStart)
            .SumAsync(o => (decimal?)o.Total) ?? 0;

        RevenueThisMonth = await _db.Orders
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt >= monthStart)
            .SumAsync(o => (decimal?)o.Total) ?? 0;
    }

    private static DateTime IsoWeekStart(DateTime d)
    {
        var dow = (int)d.DayOfWeek;
        return d.Date.AddDays(dow == 0 ? -6 : 1 - dow);
    }
}
