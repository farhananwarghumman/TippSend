using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Admin.B2BAccounts;

[Authorize(Policy = "RequireAdmin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public IndexModel(ApplicationDbContext db) => _db = db;

    public List<B2BAccount> Accounts { get; private set; } = new();
    [BindProperty] public B2BAccount NewAccount { get; set; } = new();
    [BindProperty] public B2BAccount EditAccount { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int? EditId { get; set; }

    public async Task OnGetAsync()
        => Accounts = await _db.B2BAccounts.OrderBy(a => a.BusinessName).ToListAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccount.BusinessName))
        {
            Accounts = await _db.B2BAccounts.OrderBy(a => a.BusinessName).ToListAsync();
            return Page();
        }
        _db.B2BAccounts.Add(NewAccount);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        var account = await _db.B2BAccounts.FindAsync(EditAccount.Id);
        if (account is null) return RedirectToPage();

        account.BusinessName    = EditAccount.BusinessName;
        account.ContactName     = EditAccount.ContactName;
        account.ContactEmail    = EditAccount.ContactEmail;
        account.ContactPhone    = EditAccount.ContactPhone;
        account.Address         = EditAccount.Address;
        account.Town            = EditAccount.Town;
        account.MonthlyRetainer = EditAccount.MonthlyRetainer;
        account.PerDeliveryRate = EditAccount.PerDeliveryRate;

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var account = await _db.B2BAccounts.FindAsync(id);
        if (account != null) { account.IsActive = !account.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
