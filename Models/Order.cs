using System.ComponentModel.DataAnnotations;

namespace TippSendApp.Models;

public class Order
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string Reference { get; set; } = string.Empty;

    [MaxLength(64)]
    public string TrackingToken { get; set; } = string.Empty;

    // Sender
    [MaxLength(150)]
    public string SenderName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string SenderEmail { get; set; } = string.Empty;

    [MaxLength(30)]
    public string SenderPhone { get; set; } = string.Empty;

    [MaxLength(5)]
    public string SenderCountry { get; set; } = "IE";

    // Shop & Item
    public int? ShopId { get; set; }
    public Shop? Shop { get; set; }

    [MaxLength(200)]
    public string ShopNameOverride { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ItemDescription { get; set; } = string.Empty;

    public decimal EstimatedItemPrice { get; set; }
    public decimal BudgetCap { get; set; }
    public decimal? ActualItemPrice { get; set; }
    public decimal MarkupPct { get; set; } = 8m;

    // Recipient
    [MaxLength(150)]
    public string RecipientName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string RecipientAddress { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RecipientTown { get; set; } = string.Empty;

    [MaxLength(30)]
    public string RecipientPhone { get; set; } = string.Empty;

    // Delivery
    public DateTime DeliveryDate { get; set; }
    public TimeSpan DeliveryWindowStart { get; set; }
    public DeliveryTier Tier { get; set; }
    public PricingZone Zone { get; set; }
    public bool IsEvening { get; set; }
    public bool IsSunday { get; set; }

    // Card
    [MaxLength(220)]
    public string CardMessage { get; set; } = string.Empty;

    // Pricing
    public decimal ServiceFee { get; set; }
    public decimal EveningSundaySurcharge { get; set; }
    public decimal ItemMarkupAmount { get; set; }
    public decimal Total { get; set; }

    // Order type
    public OrderType OrderType { get; set; } = OrderType.GiftDeliver;

    // Status & fulfillment
    public OrderStatus Status { get; set; } = OrderStatus.Queued;

    [MaxLength(500)]
    public string? DeliveryPhotoPath { get; set; }

    [MaxLength(500)]
    public string? DriverNotes { get; set; }

    public int? Rating { get; set; }

    public decimal? KmDriven { get; set; }

    // B2B
    public int? B2BAccountId { get; set; }
    public B2BAccount? B2BAccount { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CollectedAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // Computed helpers (not mapped)
    public string TierLabel => Tier switch
    {
        DeliveryTier.Standard => "Standard ±90 min",
        DeliveryTier.Precision => "Precision ±30 min",
        DeliveryTier.ExactMoment => "Exact Moment ±5 min",
        _ => string.Empty
    };

    public string TierCode => Tier switch
    {
        DeliveryTier.Standard => "STD",
        DeliveryTier.Precision => "PRE",
        DeliveryTier.ExactMoment => "EXM",
        _ => "STD"
    };

    public string WindowLabel
    {
        get
        {
            var start = DeliveryWindowStart;
            var minutes = Tier switch
            {
                DeliveryTier.Standard => 90,
                DeliveryTier.Precision => 30,
                DeliveryTier.ExactMoment => 5,
                _ => 30
            };
            var s = $"{(int)start.TotalHours:D2}:{start.Minutes:D2}";
            var e = start.Add(TimeSpan.FromMinutes(minutes));
            return $"{s}–{(int)e.TotalHours:D2}:{e.Minutes:D2}";
        }
    }

    public string StatusBadgeClass => Status switch
    {
        OrderStatus.Queued => "bg-secondary",
        OrderStatus.Confirmed => "bg-info",
        OrderStatus.Collected => "bg-primary",
        OrderStatus.InTransit => "bg-warning text-dark",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Failed => "bg-danger",
        _ => "bg-secondary"
    };
}
