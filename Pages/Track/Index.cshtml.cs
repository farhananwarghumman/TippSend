using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Track;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly TippSendApp.Services.PilotService _pilot;
    public IndexModel(ApplicationDbContext db, TippSendApp.Services.PilotService pilot) { _db=db; _pilot=pilot; }

    [BindProperty(SupportsGet = true)] public string? Token { get; set; }
    public Order? Order { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty] public int Rating { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) return;

        Token = Token.Trim();
        if (Uri.TryCreate(Token, UriKind.Absolute, out var trackingUrl))
            Token = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(trackingUrl.Query)["token"].ToString();
        if (!System.Text.RegularExpressions.Regex.IsMatch(Token ?? "", "^[a-fA-F0-9]{32}$"))
        {
            ErrorMessage = "Enter the tracking code or link from your delivery confirmation. For a pending request, contact us with your request reference.";
            return;
        }
        if (_pilot.Preview)
        {
            ErrorMessage = "Live order tracking is not connected in this local preview.";
            return;
        }
        Order = await _db.Orders
            .Include(o => o.Shop)
            .FirstOrDefaultAsync(o => o.TrackingToken == Token);

        if (Order is null) ErrorMessage = "Order not found. Please check your tracking link.";
    }

    public async Task<IActionResult> OnPostRateAsync(string token, int rating)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.TrackingToken == token);
        if (order is not null && rating >= 1 && rating <= 5)
        {
            order.Rating = rating;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { token });
    }
}
