using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.Admin.Pricing;

[Authorize(Policy = "RequireAdmin")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
