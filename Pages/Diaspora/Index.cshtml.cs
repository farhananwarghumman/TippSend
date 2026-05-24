using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.Diaspora;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectPermanent("/gift");
}
