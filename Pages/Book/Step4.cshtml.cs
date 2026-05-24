using TippSendApp.Data;
using TippSendApp.Extensions;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TippSendApp.Pages.Book;

public class Step4Model : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly PricingService _pricing;
    private readonly OrderService _orderService;
    private readonly StripeService _stripe;
    private readonly IDistributedCache _cache;

    public Step4Model(
        ApplicationDbContext db,
        PricingService pricing,
        OrderService orderService,
        StripeService stripe,
        IDistributedCache cache)
    {
        _db = db;
        _pricing = pricing;
        _orderService = orderService;
        _stripe = stripe;
        _cache = cache;
    }

    [BindProperty] public string CardMessage { get; set; } = string.Empty;

    public BookingSession? Session { get; private set; }
    public string ShopDisplayName { get; private set; } = string.Empty;
    public decimal ServiceFee { get; private set; }
    public decimal Surcharge { get; private set; }
    public decimal ItemMarkup { get; private set; }
    public decimal EstimatedTotal { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Session = HttpContext.Session.GetObject<BookingSession>("Booking");
        if (Session is null) return RedirectToPage("Step1");
        CardMessage = Session.CardMessage;
        await LoadDisplayDataAsync(Session);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Session = HttpContext.Session.GetObject<BookingSession>("Booking");
        if (Session is null) return RedirectToPage("Step1");

        if (CardMessage.Length > 220)
            ModelState.AddModelError(nameof(CardMessage), "Message must be 220 characters or fewer.");

        await LoadDisplayDataAsync(Session);
        if (!ModelState.IsValid) return Page();

        Session.CardMessage = CardMessage;
        HttpContext.Session.SetObject("Booking", Session);

        // ── Stripe path ──────────────────────────────────────────────────────
        if (_stripe.IsConfigured)
        {
            var pendingToken = Guid.NewGuid().ToString("N");

            await _cache.SetStringAsync(
                $"pending:{pendingToken}",
                JsonSerializer.Serialize(Session),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var stripeSession = await _stripe.CreateCheckoutSessionAsync(
                Session,
                pendingToken,
                successUrl: $"{baseUrl}/Book/PaymentReturn?session_id={{CHECKOUT_SESSION_ID}}",
                cancelUrl:  $"{baseUrl}/Book/Step4",
                ServiceFee, Surcharge, ItemMarkup);

            return Redirect(stripeSession.Url);
        }

        // ── Direct path (Stripe not yet configured) ──────────────────────────
        var order = await _orderService.CreateFromSessionAsync(Session);
        HttpContext.Session.Remove("Booking");
        return RedirectToPage("Confirmation", new { token = order.TrackingToken });
    }

    private async Task LoadDisplayDataAsync(BookingSession session)
    {
        if (session.ShopId.HasValue)
        {
            var shop = await _db.Shops.FindAsync(session.ShopId.Value);
            ShopDisplayName = shop is not null ? $"{shop.Name}, {shop.Town}" : session.ShopNameOverride;
        }
        else
        {
            ShopDisplayName = string.IsNullOrWhiteSpace(session.ShopNameOverride)
                ? "TippSend will source locally"
                : session.ShopNameOverride;
        }

        var window = new TimeSpan(session.DeliveryHour, session.DeliveryMinute, 0);
        ServiceFee = _pricing.GetServiceFee(session.Tier, session.Zone);
        Surcharge = _pricing.GetSurcharge(session.DeliveryDate, window);
        ItemMarkup = Math.Round(session.EstimatedItemPrice * 0.08m, 2);
        EstimatedTotal = session.EstimatedItemPrice + ItemMarkup + ServiceFee + Surcharge;
    }
}
