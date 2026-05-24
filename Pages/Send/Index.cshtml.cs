using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;

namespace TippSendApp.Pages.Send;

public class IndexModel : PageModel
{
    private readonly StripeService _stripe;
    private readonly EmailService _email;
    private readonly IDistributedCache _cache;
    private readonly AppSettingsService _appSettings;

    public IndexModel(StripeService stripe, EmailService email, IDistributedCache cache, AppSettingsService appSettings)
    {
        _stripe = stripe;
        _email = email;
        _cache = cache;
        _appSettings = appSettings;
    }

    public bool OperatingAllDays { get; private set; }
    public int MinBookingNoticeHours { get; private set; }

    [BindProperty, Required(ErrorMessage = "Please enter the pickup address.")]
    public string PickupAddress { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "We couldn't detect your area — please select it manually.")]
    public string PickupZone { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter the drop-off address.")]
    public string DropoffAddress { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "We couldn't detect your area — please select it manually.")]
    public string DropoffZone { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please describe the item.")]
    public string ItemDescription { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please select a preferred date.")]
    public string PreferredDate { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please select a preferred time.")]
    public string PreferredTime { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter your name.")]
    public string ContactName { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter your email."), EmailAddress]
    public string ContactEmail { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter your phone number.")]
    public string ContactPhone { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter the recipient's name.")]
    public string RecipientName { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter the recipient's phone number.")]
    public string RecipientPhone { get; set; } = "";

    [BindProperty]
    public string SpecialInstructions { get; set; } = "";

    [BindProperty, Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm your item fits in a standard car boot.")]
    public bool SizeAcknowledged { get; set; }

    public string MinDate
    {
        get
        {
            var minTime = DateTime.Now.AddHours(MinBookingNoticeHours);
            return minTime.Date.ToString("yyyy-MM-dd");
        }
    }

    public void OnGet()
    {
        var settings = _appSettings.Get();
        OperatingAllDays = settings.OperatingAllDays;
        MinBookingNoticeHours = settings.MinBookingNoticeHours;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var settings = _appSettings.Get();
        OperatingAllDays = settings.OperatingAllDays;
        MinBookingNoticeHours = settings.MinBookingNoticeHours;

        if (!ModelState.IsValid) return Page();

        var price = CalculatePrice(PickupZone, DropoffZone);

        var booking = new PickupDropBooking
        {
            PickupAddress        = PickupAddress,
            PickupZone           = PickupZone,
            DropoffAddress       = DropoffAddress,
            DropoffZone          = DropoffZone,
            ItemDescription      = ItemDescription,
            PreferredDate        = PreferredDate,
            PreferredTime        = PreferredTime,
            ContactName          = ContactName,
            ContactEmail         = ContactEmail,
            ContactPhone         = ContactPhone,
            RecipientName        = RecipientName,
            RecipientPhone       = RecipientPhone,
            SpecialInstructions  = SpecialInstructions,
            Price                = price
        };

        // ── Stripe path ──────────────────────────────────────────────────────────
        if (_stripe.IsConfigured)
        {
            var token = Guid.NewGuid().ToString("N");
            await _cache.SetStringAsync(
                $"pickup:{token}",
                JsonSerializer.Serialize(booking),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });

            var dateDisplay = DateTime.TryParse(PreferredDate, out var d)
                ? d.ToString("ddd d MMM") : PreferredDate;
            var timeDisplay = TimeSpan.TryParse(PreferredTime, out var t)
                ? DateTime.Today.Add(t).ToString("h:mm tt") : PreferredTime;

            var description = $"{ItemDescription} · {dateDisplay} at {timeDisplay} · {PickupAddress} → {DropoffAddress}";
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var session = await _stripe.CreatePickupDropSessionAsync(
                description, price, ContactEmail, token,
                successUrl: $"{baseUrl}/send/return?token={token}",
                cancelUrl:  $"{baseUrl}/send");

            return Redirect(session.Url);
        }

        // ── Fallback: email notification if Stripe not configured ────────────────
        await SendNotificationEmailAsync(booking, _email);
        return RedirectToPage("/Index");
    }

    public static decimal CalculatePrice(string pickupZone, string dropoffZone)
    {
        return pickupZone == dropoffZone
            ? pickupZone switch { "A" => 6m, "B" => 10m, "C" => 14m, _ => 12m }
            : 12m;
    }

    public static async Task SendNotificationEmailAsync(PickupDropBooking b, EmailService email)
    {
        var dateDisplay = DateTime.TryParse(b.PreferredDate, out var d)
            ? d.ToString("dddd d MMMM yyyy") : b.PreferredDate;
        var timeDisplay = TimeSpan.TryParse(b.PreferredTime, out var t)
            ? DateTime.Today.Add(t).ToString("h:mm tt") : b.PreferredTime;

        var areaName = (string z) => z switch
        {
            "A" => "Clonmel",
            "B" => "Mid Tipperary",
            "C" => "Outer Tipperary",
            _   => z
        };

        var specialRow = string.IsNullOrWhiteSpace(b.SpecialInstructions) ? "" : $"""
            <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Instructions</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.SpecialInstructions)}</td></tr>
            """;

        var html = $"""
            <!DOCTYPE html><html>
            <body style="font-family:sans-serif;background:#F6F0E2;margin:0;padding:40px 0">
            <div style="max-width:520px;margin:0 auto;background:#FAF6EC;border-radius:12px;overflow:hidden">
              <div style="background:#1F3A2E;padding:24px;text-align:center">
                <div style="font-size:22px;color:#C9A24B;font-family:Georgia,serif;font-style:italic">TippSend</div>
                <div style="color:rgba(246,240,226,0.6);font-size:11px;margin-top:4px;letter-spacing:0.1em;text-transform:uppercase">New Pickup &amp; Drop — Paid</div>
              </div>
              <div style="padding:32px">
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363;width:42%">Pickup</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.PickupAddress)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Pickup area</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{areaName(b.PickupZone)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Drop-off</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.DropoffAddress)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Drop-off area</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{areaName(b.DropoffZone)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Item</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.ItemDescription)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Date</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{dateDisplay}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Time</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{timeDisplay}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Customer</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.ContactName)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Customer phone</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.ContactPhone)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Recipient</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.RecipientName)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Recipient phone</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{System.Net.WebUtility.HtmlEncode(b.RecipientPhone)}</td></tr>
                  {specialRow}
                  <tr><td style="padding:14px 0;color:#7A7363;font-weight:600">Total paid</td><td style="padding:14px 0;text-align:right;font-family:monospace;font-size:20px;color:#C9A24B;font-weight:600">€{b.Price:F2}</td></tr>
                </table>
              </div>
            </div>
            </body></html>
            """;

        await email.SendEnquiryAsync($"New Pickup & Drop Booking — {b.ContactName}", html);
    }
}
