using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.ChristmasPage;

public class IndexModel : PageModel
{
    public DateTime TargetDate { get; private set; }

    public void OnGet()
    {
        var year = DateTime.Today.Year;
        var c = new DateTime(year, 12, 25);
        TargetDate = c < DateTime.Today ? new DateTime(year + 1, 12, 25) : c;
    }
}
