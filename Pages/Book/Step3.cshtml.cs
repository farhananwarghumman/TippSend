using TippSendApp.Extensions;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.Book;

public class Step3Model : PageModel
{
    private readonly PricingService _pricing;
    public Step3Model(PricingService pricing) => _pricing = pricing;

    [BindProperty] public DateTime DeliveryDate { get; set; } = DateTime.Today.AddDays(1);
    [BindProperty] public int DeliveryHour { get; set; } = 14;
    [BindProperty] public int DeliveryMinute { get; set; } = 0;
    [BindProperty] public DeliveryTier Tier { get; set; } = DeliveryTier.Precision;
    [BindProperty] public PricingZone Zone { get; set; } = PricingZone.A;

    public List<(DateTime Date, string Label, bool IsSunday)> DateOptions { get; private set; } = new();
    public decimal ServiceFee { get; private set; }
    public decimal Surcharge { get; private set; }
    public decimal EstimatedItemPrice { get; private set; }
    public decimal EstimatedTotal { get; private set; }

    public IActionResult OnGet()
    {
        var session = HttpContext.Session.GetObject<BookingSession>("Booking");
        if (session is null) return RedirectToPage("Step1");

        DeliveryDate = session.DeliveryDate;
        DeliveryHour = session.DeliveryHour;
        DeliveryMinute = session.DeliveryMinute;
        Tier = session.Tier;
        Zone = session.Zone;
        EstimatedItemPrice = session.EstimatedItemPrice;

        BuildDateOptions();
        CalculatePrice();
        return Page();
    }

    public IActionResult OnPost()
    {
        var session = HttpContext.Session.GetObject<BookingSession>("Booking");
        if (session is null) return RedirectToPage("Step1");

        if (DeliveryDate < DateTime.Today)
            ModelState.AddModelError(nameof(DeliveryDate), "Delivery date must be today or later.");

        BuildDateOptions();
        EstimatedItemPrice = session.EstimatedItemPrice;
        CalculatePrice();

        if (!ModelState.IsValid) return Page();

        session.DeliveryDate = DeliveryDate;
        session.DeliveryHour = DeliveryHour;
        session.DeliveryMinute = DeliveryMinute;
        session.Tier = Tier;
        session.Zone = Zone;
        HttpContext.Session.SetObject("Booking", session);

        return RedirectToPage("Step4");
    }

    private void BuildDateOptions()
    {
        DateOptions = Enumerable.Range(0, 14)
            .Select(i => DateTime.Today.AddDays(i))
            .Select(d => (d, d.Date == DateTime.Today ? "Today" : d.ToString("ddd d MMM"), d.DayOfWeek == DayOfWeek.Sunday))
            .ToList();
    }

    private void CalculatePrice()
    {
        var window = new TimeSpan(DeliveryHour, DeliveryMinute, 0);
        ServiceFee = _pricing.GetServiceFee(Tier, Zone);
        Surcharge = _pricing.GetSurcharge(DeliveryDate, window);
        var (_, _, markup, total) = _pricing.Calculate(Tier, Zone, DeliveryDate, window, EstimatedItemPrice);
        EstimatedTotal = total;
    }

    public static IEnumerable<(int Hour, int Minute)> GetTimeSlots()
    {
        for (int h = 9; h <= 20; h++)
            foreach (int m in new[] { 0, 30 })
                yield return (h, m);
    }
}
