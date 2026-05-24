using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Admin;

public record WeekRow(string Label, int Orders, decimal Revenue);
public record MonthRow(int Year, int Month, string Label, int Orders, decimal Revenue);

[Authorize(Policy = "RequireAdmin")]
public class RevenueModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public RevenueModel(ApplicationDbContext db) => _db = db;

    public List<WeekRow> Last8Weeks { get; private set; } = new();
    public List<MonthRow> Last12Months { get; private set; } = new();
    public decimal TotalAllTime { get; private set; }
    public int OrdersAllTime { get; private set; }
    public decimal RevenueThisWeek { get; private set; }
    public int OrdersThisWeek { get; private set; }
    public decimal RevenueThisMonth { get; private set; }
    public int OrdersThisMonth { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var weekStart  = IsoWeekStart(today);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var delivered = await _db.Orders
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt.HasValue)
            .Select(o => new { o.Total, o.DeliveredAt, o.DeliveryDate })
            .ToListAsync();

        TotalAllTime   = delivered.Sum(o => o.Total);
        OrdersAllTime  = delivered.Count;

        RevenueThisWeek  = delivered.Where(o => o.DeliveredAt!.Value >= weekStart).Sum(o => o.Total);
        OrdersThisWeek   = delivered.Count(o => o.DeliveredAt!.Value >= weekStart);
        RevenueThisMonth = delivered.Where(o => o.DeliveredAt!.Value >= monthStart).Sum(o => o.Total);
        OrdersThisMonth  = delivered.Count(o => o.DeliveredAt!.Value >= monthStart);

        // Weekly buckets — last 8 ISO weeks
        var weekBuckets = delivered
            .GroupBy(o => IsoWeekStart(o.DeliveredAt!.Value))
            .OrderByDescending(g => g.Key)
            .Take(8)
            .Select(g => new WeekRow(
                Label: $"w/c {g.Key:dd MMM}",
                Orders: g.Count(),
                Revenue: g.Sum(o => o.Total)))
            .ToList();
        Last8Weeks = weekBuckets;

        // Monthly buckets — last 12 calendar months
        var monthBuckets = delivered
            .GroupBy(o => new { o.DeliveredAt!.Value.Year, o.DeliveredAt!.Value.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Take(12)
            .Select(g => new MonthRow(
                Year: g.Key.Year,
                Month: g.Key.Month,
                Label: new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Orders: g.Count(),
                Revenue: g.Sum(o => o.Total)))
            .ToList();
        Last12Months = monthBuckets;
    }

    private static DateTime IsoWeekStart(DateTime d)
    {
        var dow = (int)d.DayOfWeek;
        var offset = dow == 0 ? 6 : dow - 1;
        return d.Date.AddDays(-offset);
    }
}
