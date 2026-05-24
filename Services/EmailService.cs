using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TippSendApp.Models;

namespace TippSendApp.Services;

public class EmailService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IHttpClientFactory http, IConfiguration config, ILogger<EmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(Order order)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(order.SenderEmail)) return;
        await SendAsync(
            to: order.SenderEmail,
            subject: $"Your TippSend order is confirmed — {order.Reference}",
            html: BuildConfirmationHtml(order));
    }

    public async Task SendEnquiryAsync(string subject, string html)
    {
        if (!IsConfigured) return;
        await SendAsync(to: "hello@tippsend.ie", subject: subject, html: html);
    }

    public async Task SendStatusUpdateAsync(Order order, string statusLine)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(order.SenderEmail)) return;
        await SendAsync(
            to: order.SenderEmail,
            subject: $"Update on {order.Reference} — {statusLine}",
            html: BuildStatusHtml(order, statusLine));
    }

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Resend:ApiKey"]);

    private async Task SendAsync(string to, string subject, string html)
    {
        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config["Resend:ApiKey"]);

            var payload = new
            {
                from = $"TippSend <{_config["Resend:FromAddress"] ?? "orders@tippsend.ie"}>",
                to = new[] { to },
                subject,
                html
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Resend failed {Status}: {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed to {To}", to);
        }
    }

    private static string BuildConfirmationHtml(Order order) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Georgia,serif;background:#F6F0E2;margin:0;padding:40px 0">
        <div style="max-width:560px;margin:0 auto;background:#FAF6EC;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08)">
          <div style="background:#1F3A2E;padding:32px;text-align:center">
            <div style="font-size:26px;color:#C9A24B;font-style:italic;letter-spacing:-0.02em">TippSend</div>
            <div style="color:rgba(246,240,226,0.65);font-family:sans-serif;font-size:11px;margin-top:6px;letter-spacing:0.12em;text-transform:uppercase">Any shop. Any item. To the minute.</div>
          </div>
          <div style="padding:36px 40px">
            <h2 style="color:#1F3A2E;font-size:28px;font-weight:400;margin:0 0 6px;letter-spacing:-0.02em">Order confirmed.</h2>
            <p style="color:#7A7363;margin:0 0 28px;font-family:sans-serif;font-size:13px">Reference: <strong style="font-family:monospace;color:#1F3A2E">{order.Reference}</strong></p>
            <table style="width:100%;border-collapse:collapse;font-family:sans-serif;font-size:14px;color:#14110C">
              <tr><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;color:#7A7363;width:40%">Item</td><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;text-align:right">{order.ItemDescription}</td></tr>
              <tr><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">To</td><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;text-align:right">{order.RecipientName}</td></tr>
              <tr><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Address</td><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;text-align:right">{order.RecipientAddress}, {order.RecipientTown}</td></tr>
              <tr><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Window</td><td style="padding:11px 0;border-bottom:1px solid #DDD4BA;text-align:right;font-family:monospace">{order.DeliveryDate:ddd d MMM} · {order.WindowLabel}</td></tr>
              <tr><td style="padding:14px 0;color:#7A7363">Total paid</td><td style="padding:14px 0;text-align:right;font-family:monospace;font-size:20px;color:#C9A24B;font-weight:600">€{order.Total:F2}</td></tr>
            </table>
            <div style="background:#ECE2C7;border-radius:8px;padding:16px;margin:24px 0;font-family:sans-serif;font-size:13px;color:#7A7363">
              We'll send you a WhatsApp when we collect the item. A photo arrives the moment it's at the door.
            </div>
            <div style="text-align:center;margin-top:28px">
              <a href="https://tippsend.ie/Track?token={order.TrackingToken}" style="background:#1F3A2E;color:#F6F0E2;text-decoration:none;padding:14px 36px;border-radius:999px;font-family:sans-serif;font-size:14px;font-weight:600;display:inline-block">Track your order →</a>
            </div>
          </div>
          <div style="background:#ECE2C7;padding:18px;text-align:center;font-family:sans-serif;font-size:11px;color:#7A7363;letter-spacing:0.04em">
            TippSend · Clonmel, Co. Tipperary · tippsend.ie
          </div>
        </div>
        </body>
        </html>
        """;

    private static string BuildStatusHtml(Order order, string statusLine) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;background:#F6F0E2;margin:0;padding:40px 0">
        <div style="max-width:480px;margin:0 auto;background:#FAF6EC;border-radius:12px;overflow:hidden">
          <div style="background:#1F3A2E;padding:24px;text-align:center">
            <div style="font-size:22px;color:#C9A24B;font-family:Georgia,serif;font-style:italic">TippSend</div>
          </div>
          <div style="padding:32px">
            <p style="font-size:16px;color:#14110C;margin:0 0 16px">{statusLine}</p>
            <p style="font-size:13px;color:#7A7363;margin:0 0 24px">Order: <strong style="font-family:monospace;color:#1F3A2E">{order.Reference}</strong></p>
            <a href="https://tippsend.ie/Track?token={order.TrackingToken}" style="background:#C9A24B;color:#14271F;text-decoration:none;padding:12px 28px;border-radius:999px;font-size:14px;font-weight:600;display:inline-block">Track live →</a>
          </div>
        </div>
        </body>
        </html>
        """;
}
