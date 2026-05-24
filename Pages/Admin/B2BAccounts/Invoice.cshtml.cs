using TippSendApp.Data;
using TippSendApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TippSendApp.Pages.Admin.B2BAccounts;

public record InvoiceOrder(string Reference, DateTime DeliveryDate, string Item, string Recipient, decimal Total);

[Authorize(Policy = "RequireAdmin")]
public class InvoiceModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public InvoiceModel(ApplicationDbContext db) => _db = db;

    public List<B2BAccount> Accounts { get; private set; } = new();
    public B2BAccount? SelectedAccount { get; private set; }
    public List<InvoiceOrder> InvoiceOrders { get; private set; } = new();
    public decimal DeliverySubtotal { get; private set; }
    public decimal InvoiceTotal { get; private set; }
    public string MonthLabel { get; private set; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public int? AccountId { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public int? Year { get; set; }
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)] public int? Month { get; set; }

    public async Task OnGetAsync()
    {
        Accounts = await _db.B2BAccounts.Where(a => a.IsActive).OrderBy(a => a.BusinessName).ToListAsync();

        if (AccountId.HasValue)
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == AccountId.Value);

        var now = DateTime.Today;
        Year ??= now.Year;
        Month ??= now.Month;

        MonthLabel = new DateTime(Year.Value, Month.Value, 1).ToString("MMMM yyyy");

        if (SelectedAccount is not null)
        {
            var from = new DateTime(Year.Value, Month.Value, 1);
            var to = from.AddMonths(1);

            InvoiceOrders = await _db.Orders
                .Where(o => o.B2BAccountId == SelectedAccount.Id
                         && o.DeliveryDate >= from && o.DeliveryDate < to
                         && o.Status == OrderStatus.Delivered)
                .OrderBy(o => o.DeliveryDate)
                .Select(o => new InvoiceOrder(o.Reference, o.DeliveryDate, o.ItemDescription, o.RecipientName, o.Total))
                .ToListAsync();

            DeliverySubtotal = InvoiceOrders.Sum(o => o.Total);
            InvoiceTotal = SelectedAccount.MonthlyRetainer + DeliverySubtotal;
        }
    }
}
