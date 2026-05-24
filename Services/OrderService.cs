using TippSendApp.Data;
using TippSendApp.Models;

namespace TippSendApp.Services;

public class OrderService
{
    private readonly ApplicationDbContext _db;
    private readonly PricingService _pricing;
    private readonly EmailService _email;
    private readonly WhatsAppService _whatsApp;

    public OrderService(
        ApplicationDbContext db,
        PricingService pricing,
        EmailService email,
        WhatsAppService whatsApp)
    {
        _db = db;
        _pricing = pricing;
        _email = email;
        _whatsApp = whatsApp;
    }

    public async Task<Order> CreateFromSessionAsync(BookingSession session)
    {
        var windowStart = new TimeSpan(session.DeliveryHour, session.DeliveryMinute, 0);

        var (fee, surcharge, markup, total) = _pricing.Calculate(
            session.Tier, session.Zone, session.DeliveryDate, windowStart, session.EstimatedItemPrice);

        var order = new Order
        {
            TrackingToken     = Guid.NewGuid().ToString("N"),
            ShopId            = session.ShopId,
            ShopNameOverride  = session.ShopNameOverride,
            ItemDescription   = session.ItemDescription,
            EstimatedItemPrice = session.EstimatedItemPrice,
            BudgetCap         = session.BudgetCap,
            MarkupPct         = 8m,
            SenderName        = session.SenderName,
            SenderEmail       = session.SenderEmail,
            SenderPhone       = session.SenderPhone,
            SenderCountry     = session.SenderCountry,
            RecipientName     = session.RecipientName,
            RecipientAddress  = session.RecipientAddress,
            RecipientTown     = session.RecipientTown,
            RecipientPhone    = session.RecipientPhone,
            DeliveryDate      = session.DeliveryDate,
            DeliveryWindowStart = windowStart,
            Tier              = session.Tier,
            Zone              = session.Zone,
            IsEvening         = windowStart.Hours >= 18,
            IsSunday          = session.DeliveryDate.DayOfWeek == DayOfWeek.Sunday,
            CardMessage       = session.CardMessage,
            ServiceFee        = fee,
            EveningSundaySurcharge = surcharge,
            ItemMarkupAmount  = markup,
            Total             = total,
            Status            = OrderStatus.Queued,
            CreatedAt         = DateTime.UtcNow
        };

        order.Reference = GenerateReference(order);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Fire notifications non-blocking — never let a notification failure break the order flow
        _ = Task.Run(async () =>
        {
            await _email.SendOrderConfirmationAsync(order);
            await _whatsApp.SendOrderConfirmedAsync(order);
        });

        return order;
    }

    public async Task UpdateStatusAsync(
        int orderId,
        OrderStatus newStatus,
        string? notes = null,
        string? photoPath = null,
        decimal? kmDriven = null)
    {
        var order = await _db.Orders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Order not found");

        order.Status = newStatus;
        if (notes is not null) order.DriverNotes = notes;
        if (photoPath is not null) order.DeliveryPhotoPath = photoPath;
        if (kmDriven.HasValue) order.KmDriven = kmDriven.Value;

        var now = DateTime.UtcNow;
        switch (newStatus)
        {
            case OrderStatus.Confirmed:  order.ConfirmedAt  = now; break;
            case OrderStatus.Collected:  order.CollectedAt  = now; break;
            case OrderStatus.InTransit:  order.InTransitAt  = now; break;
            case OrderStatus.Delivered:  order.DeliveredAt  = now; break;
        }

        await _db.SaveChangesAsync();

        // Fire WhatsApp notification for the new status
        _ = Task.Run(async () =>
        {
            switch (newStatus)
            {
                case OrderStatus.Collected:
                    await _whatsApp.SendCollectedAsync(order);
                    await _email.SendStatusUpdateAsync(order, "Item collected — on the way.");
                    break;
                case OrderStatus.InTransit:
                    await _whatsApp.SendOutForDeliveryAsync(order);
                    break;
                case OrderStatus.Delivered:
                    await _whatsApp.SendDeliveredAsync(order);
                    await _email.SendStatusUpdateAsync(order, $"Delivered to {order.RecipientName} ✓");
                    break;
            }
        });
    }

    public async Task<Order> CreateFromPickupDropAsync(PickupDropBooking b)
    {
        var deliveryDate = DateTime.TryParse(b.PreferredDate, out var d) ? d : DateTime.Today;
        var windowStart  = TimeSpan.TryParse(b.PreferredTime, out var t) ? t : TimeSpan.FromHours(12);

        var order = new Order
        {
            TrackingToken       = Guid.NewGuid().ToString("N"),
            OrderType           = OrderType.PickupDrop,
            ItemDescription     = b.ItemDescription,
            ShopNameOverride    = b.PickupAddress,
            SenderName          = b.ContactName,
            SenderEmail         = b.ContactEmail,
            SenderPhone         = b.ContactPhone,
            RecipientName       = b.RecipientName,
            RecipientAddress    = b.DropoffAddress,
            RecipientTown       = $"Zone {b.DropoffZone}",
            RecipientPhone      = b.RecipientPhone,
            CardMessage         = b.SpecialInstructions,
            DeliveryDate        = deliveryDate,
            DeliveryWindowStart = windowStart,
            Tier                = DeliveryTier.Standard,
            Zone                = PricingZone.A,
            Total               = b.Price,
            ServiceFee          = b.Price,
            Status              = OrderStatus.Queued,
            CreatedAt           = DateTime.UtcNow
        };

        order.Reference = $"TS·{deliveryDate:ddMMyy}·PD";
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    private static string GenerateReference(Order o) => o.OrderType == OrderType.PickupDrop
        ? $"TS·{o.DeliveryDate:ddMMyy}·PD"
        : $"TS·{o.DeliveryDate:ddMMyy}·{o.TierCode}·{o.Zone}";
}
