using TippSendApp.Data;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Stripe;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Operator", "RequireOperator");
    options.Conventions.AuthorizeFolder("/Admin",    "RequireAdmin");
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireOperator", p => p.RequireRole("Admin", "Operator"));
    options.AddPolicy("RequireAdmin",    p => p.RequireRole("Admin"));
});

// ── Session & distributed cache ───────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout             = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly         = true;
    options.Cookie.IsEssential      = true;
    options.Cookie.SecurePolicy     = CookieSecurePolicy.Always;
});

// ── HTTP clients (Resend, WhatsApp) ───────────────────────────────────────────
builder.Services.AddHttpClient();

// ── App services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton<AppSettingsService>();

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// ── Stripe webhook (minimal API — bypasses Razor Pages anti-forgery) ──────────
// Set Stripe API key once at startup
var stripeKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeKey))
    StripeConfiguration.ApiKey = stripeKey;

app.MapPost("/webhooks/stripe", async (HttpContext ctx) =>
{
    var config        = ctx.RequestServices.GetRequiredService<IConfiguration>();
    var webhookSecret = config["Stripe:WebhookSecret"];
    if (string.IsNullOrWhiteSpace(webhookSecret)) return Results.Ok();

    string json;
    using (var reader = new StreamReader(ctx.Request.Body))
        json = await reader.ReadToEndAsync();

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(
            json,
            ctx.Request.Headers["Stripe-Signature"],
            webhookSecret,
            throwOnApiVersionMismatch: false);
    }
    catch (StripeException ex)
    {
        ctx.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogWarning("Stripe signature failed: {Msg}", ex.Message);
        return Results.BadRequest();
    }

    if (stripeEvent.Type == "checkout.session.completed")
    {
        if (stripeEvent.Data.Object is not Stripe.Checkout.Session session) return Results.Ok();

        var cache = ctx.RequestServices.GetRequiredService<IDistributedCache>();

        // Skip if the return page already handled this payment
        if (await cache.GetStringAsync($"created:{session.Id}") is not null) return Results.Ok();

        if (!session.Metadata.TryGetValue("pendingToken", out var pendingToken)) return Results.Ok();

        var cachedJson = await cache.GetStringAsync($"pending:{pendingToken}");
        if (cachedJson is null) return Results.Ok();

        try
        {
            var booking = JsonSerializer.Deserialize<BookingSession>(cachedJson);
            if (booking is not null)
            {
                var orderService = ctx.RequestServices.GetRequiredService<OrderService>();
                var order = await orderService.CreateFromSessionAsync(booking);

                await cache.SetStringAsync(
                    $"created:{session.Id}", order.TrackingToken,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                    });
                await cache.RemoveAsync($"pending:{pendingToken}");
            }
        }
        catch (Exception ex)
        {
            ctx.RequestServices.GetRequiredService<ILogger<Program>>()
                .LogError(ex, "Webhook order creation failed for Stripe session {Id}", session.Id);
        }
    }

    return Results.Ok();
}).AllowAnonymous();

// ── Seed roles + admin user ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in new[] { "Admin", "Operator" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        const string adminEmail = "admin@tippsend.ie";
        const string adminPassword = "admin";

        // Remove old seed account if it still exists under the old email
        var oldAdmin = await userManager.FindByEmailAsync("admin@admin.com");
        if (oldAdmin is not null)
            await userManager.DeleteAsync(oldAdmin);

        // Ensure the correct admin account exists with the right password
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            // Ensure the existing admin has the Admin role and correct password
            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                await userManager.AddToRoleAsync(existingAdmin, "Admin");
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database seed");
    }
}

app.Run();
