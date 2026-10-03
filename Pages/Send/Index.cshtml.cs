using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace TippSendApp.Pages.Send;
public class IndexModel : PageModel
{
    private readonly PilotService _pilot;
    private readonly EmailService _email;
    private readonly IConfiguration _config;
    public IndexModel(PilotService pilot, EmailService email, IConfiguration config) { _pilot=pilot; _email=email; _config=config; }
    [BindProperty] public PilotRequest Input { get; set; } = new();
    public List<PilotRoute> Routes { get; set; } = new();
    public string? RequestReference { get; set; }
    public bool Preview => _pilot.Preview;
    public void OnGet(string? service, string? route) { Routes=_pilot.Routes(); Input.PreferredDate=PilotService.IrishNow.Date.AddDays(1); if(service is "Scheduled" or "Dedicated") Input.Service=service; if(Routes.Any(r=>r.Id==route)) Input.RouteId=route; }
    public async Task<IActionResult> OnPostAsync() {
        Routes=_pilot.Routes();
        if(Input.Service=="Scheduled") { ModelState.Remove("Input.PreferredTime"); if(string.IsNullOrEmpty(Input.RouteId)) ModelState.AddModelError("Input.RouteId","Choose an available departure."); }
        if(!ModelState.IsValid) return Page();
        try { RequestReference=_pilot.Submit(Input); }
        catch(InvalidOperationException ex) { ModelState.AddModelError("",ex.Message); return Page(); }
        TempData["PilotReference"]=RequestReference;
        var baseUrl=_config["PublicBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        if (!_pilot.Preview) {
            await _email.SendDeliveryRequestAsync(Input,$"{baseUrl}/Send/Request?token={Input.TrackingToken}");
            await _email.SendEnquiryAsync($"Delivery request — {RequestReference}",
                $"<p>A new delivery request is waiting.</p><p><a href='{baseUrl}/Admin/Dispatch'>Open dispatch</a></p>");
        }
        return RedirectToPage("Request", new { token=Input.TrackingToken });
    }
    public string? Received => TempData["PilotReference"] as string;
    // Retained for the legacy payment return page; existing paid checkouts remain usable.
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
