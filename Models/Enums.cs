namespace TippSendApp.Models;

public enum OrderType
{
    GiftDeliver,
    PickupDrop
}

public enum OrderStatus
{
    Queued,
    Confirmed,
    Collected,
    InTransit,
    Delivered,
    Failed
}

public enum DeliveryTier
{
    Standard,
    Precision,
    ExactMoment
}

public enum PricingZone
{
    A,
    B,
    C
}
