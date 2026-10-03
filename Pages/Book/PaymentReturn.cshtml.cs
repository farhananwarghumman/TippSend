using TippSendApp.Extensions;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace TippSendApp.Pages.Book;
public class PaymentReturnModel(StripeService stripe, PaymentService payments) : PageModel
{
    public bool Failed { get; private set; }
    public bool AlreadyProcessed { get; private set; }
    public async Task<IActionResult> OnGetAsync(string? session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id) || !stripe.IsConfigured) { Failed=true; return Page(); }
        try {
            var order = await payments.FulfilAsync(await stripe.GetSessionAsync(session_id));
            HttpContext.Session.Remove("Booking");
            return RedirectToPage("Confirmation", new { token=order.TrackingToken });
        } catch { Failed=true; return Page(); }
    }
}
