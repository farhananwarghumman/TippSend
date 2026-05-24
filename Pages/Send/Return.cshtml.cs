using System.Text.Json;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;

namespace TippSendApp.Pages.Send;

public class ReturnModel : PageModel
{
    private readonly StripeService _stripe;
    private readonly EmailService _email;
    private readonly OrderService _orderService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ReturnModel> _logger;

    public ReturnModel(StripeService stripe, EmailService email, OrderService orderService, IDistributedCache cache, ILogger<ReturnModel> logger)
    {
        _stripe = stripe;
        _email = email;
        _orderService = orderService;
        _cache = cache;
        _logger = logger;
    }

    public bool Failed { get; private set; }
    public PickupDropBooking? Booking { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? token, string? session_id)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Failed = true;
            return Page();
        }

        // Require a Stripe session_id — prevents payment bypass
        if (string.IsNullOrWhiteSpace(session_id))
        {
            Failed = true;
            return Page();
        }

        // Idempotency — already processed
        if (await _cache.GetStringAsync($"pickup:done:{token}") is not null)
            return Page();

        try
        {
            var stripeSession = await _stripe.GetSessionAsync(session_id);
            if (stripeSession.PaymentStatus != "paid")
            {
                Failed = true;
                return Page();
            }

            var json = await _cache.GetStringAsync($"pickup:{token}");
            if (json is null) return Page();

            Booking = JsonSerializer.Deserialize<PickupDropBooking>(json);
            if (Booking is null) { Failed = true; return Page(); }

            // Create a tracked order in the database
            var order = await _orderService.CreateFromPickupDropAsync(Booking);

            // Notify operator and send customer confirmation email
            await IndexModel.SendNotificationEmailAsync(Booking, _email);
            _ = Task.Run(async () => await _email.SendOrderConfirmationAsync(order));

            await _cache.SetStringAsync(
                $"pickup:done:{token}", "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4) });
            await _cache.RemoveAsync($"pickup:{token}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send return failed for token {Token}", token);
            Failed = true;
        }

        return Page();
    }
}
