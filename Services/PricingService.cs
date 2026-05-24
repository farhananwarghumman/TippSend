using TippSendApp.Models;

namespace TippSendApp.Services;

public class PricingService
{
    private static readonly decimal[,] ServiceFees =
    {
        // Zone A,  B,   C
        { 8m,  12m, 15m },   // Standard
        { 12m, 18m, 22m },   // Precision
        { 18m, 25m, 30m },   // ExactMoment
    };

    private const decimal EveningSundaySurcharge = 8m;
    private const decimal DefaultMarkupPct = 8m;

    public decimal GetServiceFee(DeliveryTier tier, PricingZone zone)
        => ServiceFees[(int)tier, (int)zone];

    public decimal GetSurcharge(DateTime date, TimeSpan windowStart)
    {
        bool isEvening = windowStart.Hours >= 18;
        bool isSunday = date.DayOfWeek == DayOfWeek.Sunday;
        return (isEvening || isSunday) ? EveningSundaySurcharge : 0m;
    }

    public (decimal serviceFee, decimal surcharge, decimal markup, decimal total)
        Calculate(DeliveryTier tier, PricingZone zone, DateTime date, TimeSpan windowStart, decimal estimatedItemPrice)
    {
        var fee = GetServiceFee(tier, zone);
        var surcharge = GetSurcharge(date, windowStart);
        var markupPct = DefaultMarkupPct / 100m;
        var markupAmt = Math.Round(estimatedItemPrice * markupPct, 2);
        var total = estimatedItemPrice + markupAmt + fee + surcharge;
        return (fee, surcharge, markupAmt, total);
    }

    public static int GetPrecisionMinutes(DeliveryTier tier) => tier switch
    {
        DeliveryTier.Standard => 90,
        DeliveryTier.Precision => 30,
        DeliveryTier.ExactMoment => 5,
        _ => 30
    };

    public static string GetTierBadgeClass(DeliveryTier tier) => tier switch
    {
        DeliveryTier.Standard => "tier-standard",
        DeliveryTier.Precision => "tier-precision",
        DeliveryTier.ExactMoment => "tier-exact",
        _ => "tier-standard"
    };
}
