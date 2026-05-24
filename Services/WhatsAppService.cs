using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TippSendApp.Models;

namespace TippSendApp.Services;

public class WhatsAppService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IHttpClientFactory http, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_config["WhatsApp:AccessToken"]) &&
        !string.IsNullOrWhiteSpace(_config["WhatsApp:PhoneNumberId"]);

    private string ApiUrl =>
        $"https://graph.facebook.com/{_config["WhatsApp:ApiVersion"] ?? "v19.0"}/{_config["WhatsApp:PhoneNumberId"]}/messages";

    public Task SendOrderConfirmedAsync(Order order) =>
        Send(order.SenderPhone, $"""
            ✅ Order received — #{order.Reference}

            Item: {order.ItemDescription}
            To: {order.RecipientName}, {order.RecipientTown}
            Window: {order.DeliveryDate:ddd d MMM} · {order.WindowLabel}

            We'll text you when we collect from the shop. 🌿

            Track: https://tippsend.ie/Track?token={order.TrackingToken}
            """);

    public Task SendCollectedAsync(Order order) =>
        Send(order.SenderPhone, $"""
            📦 Item collected ✅

            {order.ItemDescription} is in hand — on the way to {order.RecipientTown}.
            """);

    public Task SendOutForDeliveryAsync(Order order) =>
        Send(order.SenderPhone, $"""
            🚐 Out for delivery

            Heading to {order.RecipientAddress}, {order.RecipientTown}.
            Arriving within the {order.WindowLabel} window.
            """);

    public Task SendDeliveredAsync(Order order) =>
        Send(order.SenderPhone, $"""
            ✨ Delivered to {order.RecipientName} at {order.DeliveredAt?.ToLocalTime():HH:mm}.

            Rate your delivery (takes 5 seconds):
            https://tippsend.ie/Track?token={order.TrackingToken}
            """);

    private async Task Send(string rawPhone, string text)
    {
        if (!IsConfigured) return;
        var phone = NormalisePhone(rawPhone);
        if (string.IsNullOrWhiteSpace(phone)) return;

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config["WhatsApp:AccessToken"]);

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "text",
                text = new { body = text.Trim() }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(ApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("WhatsApp send failed to {Phone}: {Status} {Body}", phone, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp failed for phone {Phone}", phone);
        }
    }

    // Converts Irish local numbers to E.164 (+353...)
    private static string NormalisePhone(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (cleaned.StartsWith("00")) cleaned = "+" + cleaned[2..];
        if (cleaned.StartsWith("0")) cleaned = "+353" + cleaned[1..];
        if (!cleaned.StartsWith("+")) cleaned = "+" + cleaned;
        return cleaned;
    }
}
