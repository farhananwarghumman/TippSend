using TippSendApp.Data;
using TippSendApp.Extensions;
using TippSendApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Book;

public class Step1Model : PageModel
{
    private readonly ApplicationDbContext _db;
    public Step1Model(ApplicationDbContext db) => _db = db;

    [BindProperty] public int? ShopId { get; set; }
    [BindProperty] public string ShopNameOverride { get; set; } = string.Empty;
    [BindProperty] public string ItemDescription { get; set; } = string.Empty;
    [BindProperty] public decimal EstimatedItemPrice { get; set; }
    [BindProperty] public decimal BudgetCap { get; set; }
    [BindProperty(SupportsGet = true)] public string? PreselectedDate { get; set; }
    [BindProperty(SupportsGet = true)] public string? PreselectedShop { get; set; }
    [BindProperty(SupportsGet = true)] public string? PreselectedItem { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? PreselectedPrice { get; set; }

    public List<SelectListItem> ShopOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadShopsAsync();
        var session = HttpContext.Session.GetObject<BookingSession>("Booking") ?? new BookingSession();

        // Honour date pre-selected from Gift page
        if (!string.IsNullOrWhiteSpace(PreselectedDate) && DateTime.TryParse(PreselectedDate, out var preDate) && preDate >= DateTime.Today)
            session.DeliveryDate = preDate;

        // Pre-fill from product card click
        if (!string.IsNullOrWhiteSpace(PreselectedShop))  session.ShopNameOverride   = PreselectedShop;
        if (!string.IsNullOrWhiteSpace(PreselectedItem))  session.ItemDescription    = PreselectedItem;
        if (PreselectedPrice is > 0)                      session.EstimatedItemPrice = PreselectedPrice.Value;

        ShopId = session.ShopId;
        ShopNameOverride = session.ShopNameOverride;
        ItemDescription = session.ItemDescription;
        EstimatedItemPrice = session.EstimatedItemPrice;
        BudgetCap = session.BudgetCap;
        HttpContext.Session.SetObject("Booking", session);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadShopsAsync();
        if (string.IsNullOrWhiteSpace(ItemDescription))
            ModelState.AddModelError(nameof(ItemDescription), "Please describe the item.");
        if (EstimatedItemPrice <= 0)
            ModelState.AddModelError(nameof(EstimatedItemPrice), "Please enter an estimated price.");
        if (!ModelState.IsValid) return Page();

        var session = HttpContext.Session.GetObject<BookingSession>("Booking") ?? new BookingSession();
        session.ShopId = ShopId;
        session.ShopNameOverride = ShopNameOverride;
        session.ItemDescription = ItemDescription;
        session.EstimatedItemPrice = EstimatedItemPrice;
        session.BudgetCap = BudgetCap > 0 ? BudgetCap : EstimatedItemPrice * 1.2m;
        HttpContext.Session.SetObject("Booking", session);

        return RedirectToPage("Step2");
    }

    private async Task LoadShopsAsync()
    {
        var shops = await _db.Shops.Where(s => s.IsActive).OrderBy(s => s.Town).ThenBy(s => s.Name).ToListAsync();
        ShopOptions = shops.Select(s => new SelectListItem($"{s.Name} — {s.Town}", s.Id.ToString())).ToList();
        ShopOptions.Insert(0, new SelectListItem("— I'll describe what I need —", ""));
    }
}
