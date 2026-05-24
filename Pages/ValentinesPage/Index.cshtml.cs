using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.ValentinesPage;

public class IndexModel : PageModel
{
    public DateTime TargetDate { get; private set; }

    public void OnGet()
    {
        var year = DateTime.Today.Year;
        var v = new DateTime(year, 2, 14);
        TargetDate = v < DateTime.Today ? new DateTime(year + 1, 2, 14) : v;
    }
}
