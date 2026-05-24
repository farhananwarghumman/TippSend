namespace TippSendApp.Models;

public class BookingSession
{
    public int? ShopId { get; set; }
    public string ShopNameOverride { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal EstimatedItemPrice { get; set; }
    public decimal BudgetCap { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string SenderCountry { get; set; } = "IE";

    public string RecipientName { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string RecipientTown { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;

    public DateTime DeliveryDate { get; set; } = DateTime.Today.AddDays(1);
    public int DeliveryHour { get; set; } = 14;
    public int DeliveryMinute { get; set; } = 0;
    public DeliveryTier Tier { get; set; } = DeliveryTier.Precision;
    public PricingZone Zone { get; set; } = PricingZone.A;

    public string CardMessage { get; set; } = string.Empty;
}
