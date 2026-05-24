using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.MothersDayPage;

public class IndexModel : PageModel
{
    public DateTime TargetDate { get; private set; }

    public void OnGet()
    {
        // Second Sunday in May
        var year = DateTime.Today.Month > 5 ? DateTime.Today.Year + 1 : DateTime.Today.Year;
        TargetDate = GetMothersDaySunday(year);
        if (TargetDate < DateTime.Today)
            TargetDate = GetMothersDaySunday(year + 1);
    }

    private static DateTime GetMothersDaySunday(int year)
    {
        var may1 = new DateTime(year, 5, 1);
        var offset = (DayOfWeek.Sunday - may1.DayOfWeek + 7) % 7;
        return may1.AddDays(offset + 7); // second Sunday
    }
}
