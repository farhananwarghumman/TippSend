using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Track;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Token { get; set; }
    public Order? Order { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty] public int Rating { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Token)) return;

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
