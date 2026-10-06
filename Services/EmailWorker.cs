using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSendApp.Data;
namespace TippSendApp.Services;

public class EmailWorker(IServiceScopeFactory scopes, IConfiguration config, IHttpClientFactory http, ILogger<EmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(30));
        while(await timer.WaitForNextTickAsync(stoppingToken)) {
            if(string.IsNullOrWhiteSpace(config["Resend:ApiKey"])) continue;
            try {
                using var scope=scopes.CreateScope();
                var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await using var tx=await db.Database.BeginTransactionAsync(stoppingToken);
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(912044)",stoppingToken);
                var messages=await db.EmailMessages.Where(m=>m.SentAt==null && m.NextAttemptAt<=DateTime.UtcNow && m.Attempts<12)
                    .OrderBy(m=>m.CreatedAt).Take(20).ToListAsync(stoppingToken);
                foreach(var m in messages) {
                    using var request=new HttpRequestMessage(HttpMethod.Post,"https://api.resend.com/emails");
                    request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",config["Resend:ApiKey"]);
                    request.Headers.Add("Idempotency-Key",m.Id.ToString());
                    request.Content=new StringContent(JsonSerializer.Serialize(new {
                        from=$"TippSend <{config["Resend:FromAddress"] ?? "orders@tippsend.ie"}>",
                        to=new[]{m.To}, subject=m.Subject, html=m.Html,
                        reply_to=config["Notifications:ReplyTo"] ?? "contact@tippsend.ie"
                    }),Encoding.UTF8,"application/json");
                    m.Attempts++;
                    try {
                        using var response=await http.CreateClient().SendAsync(request,stoppingToken);
                        if(response.IsSuccessStatusCode) { m.SentAt=DateTime.UtcNow; m.LastError=null; }
                        else { m.LastError=$"Email provider returned {(int)response.StatusCode}"; }
                    } catch(HttpRequestException) { m.LastError="Email provider could not be reached"; }
                    m.NextAttemptAt=DateTime.UtcNow.AddMinutes(Math.Min(60,Math.Pow(2,m.Attempts)));
                }
                await db.SaveChangesAsync(stoppingToken); await tx.CommitAsync(stoppingToken);
            } catch(Exception ex) when(!stoppingToken.IsCancellationRequested) { logger.LogError(ex,"Email queue processing failed"); }
        }
    }
}
