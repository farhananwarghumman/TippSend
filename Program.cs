using TippSendApp.Data;
using TippSendApp.Models;
using TippSendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Stripe;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Railway terminates HTTPS at its reverse proxy. Enable only behind that proxy.
if (builder.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = 1;
    });
var persistentDataDir = builder.Configuration["AppSettingsDir"];
if (!string.IsNullOrWhiteSpace(persistentDataDir))
{
    var keysDir = Path.Combine(persistentDataDir, "keys");
    Directory.CreateDirectory(keysDir);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDir))
        .SetApplicationName("TippSend");
}

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 12;
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
builder.Services.AddSingleton<PilotService>();
builder.Services.AddSingleton<MerchantEnquiryService>();

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("ReverseProxy:Enabled")) app.UseForwardedHeaders();
// The readiness probe returns no customer data and verifies database connectivity.
app.MapGet("/health", async (ApplicationDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment() || !builder.Configuration.GetValue<bool>("LocalPreview")) app.UseHttpsRedirection();
app.UseStaticFiles();
var uploadDirectory = builder.Configuration["UploadsDir"];
if (!string.IsNullOrWhiteSpace(uploadDirectory))
{
    Directory.CreateDirectory(uploadDirectory);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadDirectory), RequestPath = "/uploads"
    });
}
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

// ── Migrate database and ensure roles ───────────────────────────────────────────────────
if (!(app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("LocalPreview")))
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

        var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
        var adminPassword = builder.Configuration["BootstrapAdmin:Password"];
        var administrators = await userManager.GetUsersInRoleAsync("Admin");
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
            && !administrators.Any())
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            // Never turn an existing public account into an administrator automatically.
            if (admin is not null) throw new InvalidOperationException("Bootstrap email is already registered.");
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var created = await userManager.CreateAsync(admin, adminPassword);
            if (!created.Succeeded) throw new InvalidOperationException("Admin bootstrap failed: " + string.Join(", ", created.Errors.Select(error => error.Code)));
            var granted = await userManager.AddToRoleAsync(admin, "Admin");
            if (!granted.Succeeded) throw new InvalidOperationException("Admin role assignment failed.");
        }

        // Existing accounts and passwords are managed through Identity, never reset on startup.

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database seed");
        throw;
    }
}

app.Run();
