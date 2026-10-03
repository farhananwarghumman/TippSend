using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TippSendApp.Data;
using TippSendApp.Models;
using TippSendApp.Services;
namespace TippSendApp.Pages.Send;

[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public class RequestModel(PilotService pilot, StripeService stripe, PaymentService payments, ApplicationDbContext db, IConfiguration config) : PageModel
{
    [BindProperty(SupportsGet=true)] public string Token { get; set; } = "";
    public PilotRequest? Delivery { get; private set; }
    public bool CanPay => Delivery?.Status=="Accepted" && Delivery.QuotedPrice>0 && stripe.IsConfigured;
    public bool TestMode => !stripe.IsLive;
    public async Task<IActionResult> OnGetAsync()
    {
        Delivery=pilot.Find(Token);
        if (Delivery is null) return NotFound();
        var order=await db.Orders.SingleOrDefaultAsync(x=>x.TrackingToken==Token);
        if (order is not null) return RedirectToPage("/Track/Index",new { token=Token });
        return Page();
    }
    public async Task<IActionResult> OnPostPayAsync()
    {
        Delivery=pilot.Find(Token);
        if (!CanPay) { ModelState.AddModelError("", "Payment is available after we confirm your delivery and price."); return Page(); }
        var existing=await db.PaymentDrafts.SingleOrDefaultAsync(x=>x.Kind=="pilot" && x.Token==Token);
        if (existing?.OrderId is not null) return RedirectToPage("/Track/Index",new { token=Token });
        if (existing?.StripeSessionId is not null) {
            var session=await stripe.GetSessionAsync(existing.StripeSessionId);
            if(session.Status=="open") return Redirect(session.Url);
            if(session.PaymentStatus=="paid") {
                await payments.FulfilAsync(session);
                return RedirectToPage("/Track/Index",new { token=Token });
            }
            ModelState.AddModelError("", "This payment link has expired. Please contact TippSend for a new quote."); return Page();
        }
        var draft=existing ?? await payments.CreateDraftAsync("pilot",Delivery!,Delivery!.QuotedPrice!.Value,Token);
        try {
            var baseUrl=config["PublicBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
            var session=await stripe.CreatePickupDropSessionAsync($"Delivery {Delivery!.Reference}", draft.AmountCents/100m,
                Delivery.ContactEmail,draft.Token,$"{baseUrl}/Book/PaymentReturn?session_id={{CHECKOUT_SESSION_ID}}",$"{baseUrl}/Send/Request?token={Token}");
            await payments.AttachSessionAsync(draft.Token,session.Id);
            return Redirect(session.Url);
        } catch (Stripe.StripeException) {
            ModelState.AddModelError("", "We could not open payment. Please try again shortly; no booking has been marked as paid."); return Page();
        }
    }
}
