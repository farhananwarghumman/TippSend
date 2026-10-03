using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace TippSendApp.Pages.Send;
// No legacy cache-only checkouts are created by this application.
public class ReturnModel : PageModel
{
    public bool Failed => true;
    public TippSendApp.Models.PickupDropBooking? Booking => null;
    public IActionResult OnGet() => RedirectToPage("/Book/PaymentReturn", new { session_id=Request.Query["session_id"].ToString() });
}
