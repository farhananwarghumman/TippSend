using TippSendApp.Extensions;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TippSendApp.Pages.Book;

public class PaymentReturnModel : PageModel
{
    private readonly StripeService _stripe;
    private readonly OrderService _orderService;
    private readonly IDistributedCache _cache;

    public PaymentReturnModel(StripeService stripe, OrderService orderService, IDistributedCache cache)
    {
        _stripe = stripe;
        _orderService = orderService;
        _cache = cache;
    }

    public bool Failed { get; private set; }
    public bool AlreadyProcessed { get; private set; }

    public async Task<IActionResult> OnGetAsync(string session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
            return RedirectToPage("Step4");

        // Idempotency check — if webhook already created this order, don't create again
        var alreadyCreated = await _cache.GetStringAsync($"created:{session_id}");
        if (alreadyCreated is not null)
        {
            AlreadyProcessed = true;
            return Page();
        }

        try
        {
            var stripeSession = await _stripe.GetSessionAsync(session_id);

            if (stripeSession.PaymentStatus != "paid")
            {
                Failed = true;
                return Page();
            }

            // Retrieve the pending booking from cache
            if (!stripeSession.Metadata.TryGetValue("pendingToken", out var pendingToken))
            {
                Failed = true;
                return Page();
            }

            var cachedJson = await _cache.GetStringAsync($"pending:{pendingToken}");
            if (cachedJson is null)
            {
                // Cache expired or webhook already handled it
                AlreadyProcessed = true;
                return Page();
            }

            var booking = JsonSerializer.Deserialize<BookingSession>(cachedJson);
            if (booking is null)
            {
                Failed = true;
                return Page();
            }

            var order = await _orderService.CreateFromSessionAsync(booking);

            // Mark as created so the webhook doesn't duplicate
            await _cache.SetStringAsync(
                $"created:{session_id}", order.TrackingToken,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) });

            // Clean up pending token
            await _cache.RemoveAsync($"pending:{pendingToken}");

            HttpContext.Session.Remove("Booking");
            return RedirectToPage("Confirmation", new { token = order.TrackingToken });
        }
        catch (Exception)
        {
            Failed = true;
            return Page();
        }
    }
}
