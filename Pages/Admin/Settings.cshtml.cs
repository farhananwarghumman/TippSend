using TippSendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.Admin;

[Authorize(Policy = "RequireAdmin")]
public class SettingsModel : PageModel
{
    private readonly AppSettingsService _settings;

    public SettingsModel(AppSettingsService settings) => _settings = settings;

    [BindProperty] public bool OperatingAllDays { get; set; }
    [BindProperty] public int MinBookingNoticeHours { get; set; }

    public void OnGet()
    {
        var s = _settings.Get();
        OperatingAllDays = s.OperatingAllDays;
        MinBookingNoticeHours = s.MinBookingNoticeHours;
    }

    public IActionResult OnPost()
    {
        _settings.Save(new RuntimeSettings
        {
            OperatingAllDays = OperatingAllDays,
            MinBookingNoticeHours = MinBookingNoticeHours
        });
        TempData["Saved"] = true;
        return RedirectToPage();
    }
}
