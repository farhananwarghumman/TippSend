using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using TippSendApp.Data;
using TippSendApp.Models;
namespace TippSendApp.Services;

public class PaymentService(ApplicationDbContext db, OrderService orders, StripeService stripe)
{
    public async Task<PaymentDraft> CreateDraftAsync(string kind, object booking, decimal amount, string? token = null)
    {
        if (amount <= 0 || amount > 10000) throw new InvalidOperationException("A valid agreed price is required.");
        var draft = new PaymentDraft { Token = token ?? Guid.NewGuid().ToString("N"), Kind = kind,
            Json = JsonSerializer.Serialize(booking), AmountCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero) };
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({draft.Token}))");
        var existing = await db.PaymentDrafts.FindAsync(draft.Token);
        if (existing is not null) { await transaction.CommitAsync(); return existing; }
        db.Add(draft); await db.SaveChangesAsync();
        await transaction.CommitAsync(); return draft;
    }

    public async Task AttachSessionAsync(string token, string sessionId)
    {
        var draft = await db.PaymentDrafts.FindAsync(token) ?? throw new InvalidOperationException("Booking not found.");
        draft.StripeSessionId = sessionId; await db.SaveChangesAsync();
    }

    public async Task<Order> FulfilAsync(Session session)
    {
        if (session.PaymentStatus != "paid" || session.Currency != "eur" || session.Mode != "payment")
            throw new InvalidOperationException("Payment has not been verified.");
        var expectedLive = stripe.IsLive;
        if (session.Livemode != expectedLive) throw new InvalidOperationException("Payment environment mismatch.");
        if (!session.Metadata.TryGetValue("pendingToken", out var token))
            throw new InvalidOperationException("Payment has no booking reference.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({token}))");
        var draft = await db.PaymentDrafts.Include(x=>x.Order).SingleOrDefaultAsync(x=>x.Token==token)
            ?? throw new InvalidOperationException("Payment booking could not be found.");
        // Return and webhook may race; the locked durable record creates exactly one order.
        if (draft.StripeSessionId != session.Id || session.AmountTotal != draft.AmountCents)
            throw new InvalidOperationException("Payment does not match this booking.");
        if (draft.Order is not null) { await transaction.CommitAsync(); return draft.Order; }
        Order order;
        if (draft.Kind == "gift")
            order = await orders.CreateFromSessionAsync(JsonSerializer.Deserialize<BookingSession>(draft.Json)!, notify:false);
        else if (draft.Kind == "pilot")
        {
            var r = JsonSerializer.Deserialize<PilotRequest>(draft.Json)!;
            order = await orders.CreateFromPickupDropAsync(new PickupDropBooking {
                PickupAddress=r.PickupAddress + " " + r.PickupEircode,
                DropoffAddress=r.DropoffAddress + " " + r.DropoffEircode,
                DropoffZone=r.DropoffEircode, ItemDescription=r.ItemDescription,
                ContactName=r.ContactName, ContactEmail=r.ContactEmail, ContactPhone=r.ContactPhone,
                RecipientName=r.RecipientName, RecipientPhone=r.RecipientPhone,
                PreferredDate=r.PreferredDate.ToString("yyyy-MM-dd"), PreferredTime=r.PreferredTime, SpecialInstructions=r.SpecialInstructions ?? "",
                Price=draft.AmountCents / 100m });
            order.TrackingToken = r.TrackingToken;
            order.Reference = r.Reference;
        }
        else throw new InvalidOperationException("Unsupported payment booking.");
        // Charged amount is fixed at checkout, even if pricing settings change afterwards.
        order.Total = draft.AmountCents / 100m;
        order.PaymentIsTest = !session.Livemode;
        draft.OrderId = order.Id;
        await db.SaveChangesAsync();
        await orders.NotifyCreatedAsync(order);
        await transaction.CommitAsync();
        return order;
    }
}
