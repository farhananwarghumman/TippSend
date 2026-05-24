using Stripe;
using Stripe.Checkout;
using TippSendApp.Models;

namespace TippSendApp.Services;

public class StripeService
{
    private readonly IConfiguration _config;

    public StripeService(IConfiguration config)
    {
        _config = config;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Stripe:SecretKey"]);

    public async Task<Session> CreateCheckoutSessionAsync(
        BookingSession booking,
        string pendingToken,
        string successUrl,
        string cancelUrl,
        decimal serviceFee,
        decimal surcharge,
        decimal itemMarkup)
    {
        var lineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    UnitAmount = ToStripeAmount(booking.EstimatedItemPrice + itemMarkup),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = booking.ItemDescription,
                        Description = $"Item + sourcing fee"
                    }
                },
                Quantity = 1
            },
            new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    UnitAmount = ToStripeAmount(serviceFee),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Delivery — {booking.Tier} / Zone {booking.Zone}",
                        Description = $"{booking.DeliveryDate:ddd d MMM} at {booking.DeliveryHour:D2}:{booking.DeliveryMinute:D2}"
                    }
                },
                Quantity = 1
            }
        };

        if (surcharge > 0)
        {
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    UnitAmount = ToStripeAmount(surcharge),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Evening / Sunday surcharge"
                    }
                },
                Quantity = 1
            });
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems,
            CustomerEmail = booking.SenderEmail,
            Metadata = new Dictionary<string, string> { ["pendingToken"] = pendingToken },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            PaymentMethodTypes = new List<string> { "card" },
            BillingAddressCollection = "auto",
            PhoneNumberCollection = new SessionPhoneNumberCollectionOptions { Enabled = false }
        };

        return await new SessionService().CreateAsync(options);
    }

    public async Task<Session> CreatePickupDropSessionAsync(
        string description,
        decimal amount,
        string customerEmail,
        string pendingToken,
        string successUrl,
        string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = ToStripeAmount(amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "TippSend — Pickup & Drop",
                            Description = description
                        }
                    },
                    Quantity = 1
                }
            },
            CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail,
            Metadata = new Dictionary<string, string> { ["pickupToken"] = pendingToken },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            PaymentMethodTypes = new List<string> { "card" }
        };

        return await new SessionService().CreateAsync(options);
    }

    public async Task<Session> GetSessionAsync(string sessionId)
        => await new SessionService().GetAsync(sessionId);

    private static long ToStripeAmount(decimal amount)
        => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
