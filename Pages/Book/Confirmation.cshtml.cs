using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Book;

public class ConfirmationModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public ConfirmationModel(ApplicationDbContext db) => _db = db;

    public Order? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        Order = await _db.Orders
            .Include(o => o.Shop)
            .FirstOrDefaultAsync(o => o.TrackingToken == token);

        if (Order is null) return RedirectToPage("/Index");
        return Page();
    }
}
