using TippSendApp.Data;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Operator;

[Authorize(Policy = "RequireOperator")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly OrderService _orderService;
    private readonly CloudinaryService _cloudinary;

    public DashboardModel(ApplicationDbContext db, OrderService orderService, CloudinaryService cloudinary)
    {
        _db = db;
        _orderService = orderService;
        _cloudinary = cloudinary;
    }

    public List<Order> TodaysOrders { get; private set; } = new();
    public Order? SelectedOrder { get; private set; }

    // Sidebar stats
    public int TotalToday { get; private set; }
    public int ActiveToday { get; private set; }
    public int DeliveredToday { get; private set; }
    public decimal RevToday { get; private set; }

    public DateTime ViewDate { get; private set; }
    public bool IsToday => ViewDate.Date == DateTime.Today;

    [BindProperty(SupportsGet = true)] public int? SelectedOrderId { get; set; }
    [BindProperty(SupportsGet = true)] public string FilterType { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ViewDateStr { get; set; }

    [BindProperty] public OrderStatus NewStatus { get; set; }
    [BindProperty] public string? DriverNotes { get; set; }
    [BindProperty] public IFormFile? DeliveryPhoto { get; set; }
    [BindProperty] public decimal? KmDriven { get; set; }

    public async Task OnGetAsync() => await LoadDataAsync();

    public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId)
    {
        string? photoPath = null;
        if (DeliveryPhoto is { Length: > 0 })
        {
            var order = await _db.Orders.FindAsync(orderId);
            photoPath = await _cloudinary.UploadDeliveryPhotoAsync(DeliveryPhoto, order?.Reference ?? orderId.ToString());
        }
        await _orderService.UpdateStatusAsync(orderId, NewStatus, DriverNotes, photoPath, KmDriven);
        return RedirectToPage(new { SelectedOrderId = orderId, FilterType, ViewDateStr });
    }

    private async Task LoadDataAsync()
    {
        ViewDate = DateTime.TryParse(ViewDateStr, out var d) ? d.Date : DateTime.Today;

        var query = _db.Orders
            .Include(o => o.Shop)
            .Include(o => o.B2BAccount)
            .Where(o => o.DeliveryDate.Date == ViewDate);

        query = FilterType switch
        {
            "Gift"      => query.Where(o => o.OrderType == OrderType.GiftDeliver),
            "PickupDrop"=> query.Where(o => o.OrderType == OrderType.PickupDrop),
            "B2B"       => query.Where(o => o.B2BAccountId != null),
            _           => query
        };

        TodaysOrders = (await query.ToListAsync())
            .OrderBy(o => o.DeliveryWindowStart)
            .ToList();

        TotalToday    = TodaysOrders.Count;
        DeliveredToday = TodaysOrders.Count(o => o.Status == OrderStatus.Delivered);
        ActiveToday   = TodaysOrders.Count(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Failed);
        RevToday      = TodaysOrders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.Total);

        if (SelectedOrderId.HasValue)
            SelectedOrder = TodaysOrders.FirstOrDefault(o => o.Id == SelectedOrderId.Value)
                ?? await _db.Orders.Include(o => o.Shop).Include(o => o.B2BAccount).FirstOrDefaultAsync(o => o.Id == SelectedOrderId.Value);
        else if (TodaysOrders.Any())
            SelectedOrder = TodaysOrders.First();
    }
}
