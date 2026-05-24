using TippSendApp.Extensions;
using TippSendApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.Book;

public class Step2Model : PageModel
{
    [BindProperty] public string SenderName { get; set; } = string.Empty;
    [BindProperty] public string SenderEmail { get; set; } = string.Empty;
    [BindProperty] public string SenderPhone { get; set; } = string.Empty;
    [BindProperty] public string RecipientName { get; set; } = string.Empty;
    [BindProperty] public string RecipientAddress { get; set; } = string.Empty;
    [BindProperty] public string RecipientTown { get; set; } = string.Empty;
    [BindProperty] public string RecipientPhone { get; set; } = string.Empty;

    public static readonly string[] IrishTowns = new[]
    {
        "Clonmel", "Cahir", "Carrick-on-Suir", "Cashel", "Fethard",
        "Ardfinnan", "Clogheen", "Killenaule", "Kilsheelan", "Mullinahone"
    };

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetObject<BookingSession>("Booking") is null)
            return RedirectToPage("Step1");

        var session = HttpContext.Session.GetObject<BookingSession>("Booking")!;
        SenderName = session.SenderName;
        SenderEmail = session.SenderEmail;
        SenderPhone = session.SenderPhone;
        RecipientName = session.RecipientName;
        RecipientAddress = session.RecipientAddress;
        RecipientTown = session.RecipientTown;
        RecipientPhone = session.RecipientPhone;
        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(SenderName)) ModelState.AddModelError(nameof(SenderName), "Required");
        if (string.IsNullOrWhiteSpace(SenderEmail)) ModelState.AddModelError(nameof(SenderEmail), "Required");
        if (string.IsNullOrWhiteSpace(RecipientName)) ModelState.AddModelError(nameof(RecipientName), "Required");
        if (string.IsNullOrWhiteSpace(RecipientAddress)) ModelState.AddModelError(nameof(RecipientAddress), "Required");
        if (string.IsNullOrWhiteSpace(RecipientTown)) ModelState.AddModelError(nameof(RecipientTown), "Required");
        if (!ModelState.IsValid) return Page();

        var session = HttpContext.Session.GetObject<BookingSession>("Booking") ?? new BookingSession();
        session.SenderName = SenderName;
        session.SenderEmail = SenderEmail;
        session.SenderPhone = SenderPhone;
        session.RecipientName = RecipientName;
        session.RecipientAddress = RecipientAddress;
        session.RecipientTown = RecipientTown;
        session.RecipientPhone = RecipientPhone;
        HttpContext.Session.SetObject("Booking", session);

        return RedirectToPage("Step3");
    }
}
