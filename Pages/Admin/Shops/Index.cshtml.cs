using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Admin.Shops;

[Authorize(Policy = "RequireAdmin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    public List<Shop> Shops { get; private set; } = new();
    [BindProperty] public Shop NewShop { get; set; } = new();

    public async Task OnGetAsync()
        => Shops = await _db.Shops.OrderBy(s => s.Town).ThenBy(s => s.Name).ToListAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            Shops = await _db.Shops.OrderBy(s => s.Town).ThenBy(s => s.Name).ToListAsync();
            return Page();
        }
        _db.Shops.Add(NewShop);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop != null) { shop.IsActive = !shop.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var shop = await _db.Shops.FindAsync(id);
        if (shop != null) { _db.Shops.Remove(shop); await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
