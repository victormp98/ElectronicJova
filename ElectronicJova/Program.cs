
using ElectronicJova.Data;
using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using Microsoft.EntityFrameworkCore;
using ElectronicJova.DbInitializer;
using Microsoft.AspNetCore.Identity;
using Serilog;
using ElectronicJova.Utilities;
using Stripe;
using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;
using ElectronicJova.Hubs;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build())
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Logger = loggerConfig;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
});
builder.Services.AddRazorPages();
builder.Services.AddSignalR(); // Fase 3: Notificaciones en tiempo real

// Configure DbContext for MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 0)))
);

// Register UnitOfWork and IRepository
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();

// Update Identity registration to include token providers
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configurar rutas de autenticación explícitas para evitar el bug de 404 en producción
// ASP.NET Identity por default usa /Account/Login (sin /Identity/), lo que genera 404 en producción.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    // ReturnUrlParameter a la página de donde vino el usuario al intentar acceder sin sesión
    options.ReturnUrlParameter = "returnUrl";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddOptions();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["ResendSettings:ApiKey"] ?? string.Empty;
});
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, ResendEmailSender>();

// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Stripe API Key
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
var stripeSecretKey = builder.Configuration["StripeSettings:SecretKey"];
var stripeWebhookSecret = builder.Configuration["StripeSettings:WebhookSecret"];

if (string.IsNullOrEmpty(stripeSecretKey) || stripeSecretKey == "REPLACE_WITH_SECRET") {
    Log.Error("STRIPE FATAL: SecretKey no está configurado. Los pagos no funcionarán.");
    if (builder.Environment.IsDevelopment()) throw new InvalidOperationException("Stripe SecretKey missing in Development.");
} else {
    StripeConfiguration.ApiKey = stripeSecretKey;
}

if (string.IsNullOrEmpty(stripeWebhookSecret) || stripeWebhookSecret == "REPLACE_WITH_SECRET") {
    Log.Warning("STRIPE WARNING: WebhookSecret no está configurado. Las notificaciones automáticas de pago fallarán.");
}


// Add Rate Limiting (Fase 4: Seguridad)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100, // 100 requests per minute
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.OnRejected = (context, cancellationToken) =>
    {
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.HttpContext.Request.Path;
        Log.Warning("RATE LIMIT EXCEEDED: IP {IP} attempted to access {Path}", ipAddress, path);
        
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return new ValueTask();
    };
});


var app = builder.Build();

// Security Headers Middleware with inclusive CSP for Admin Panel
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    var csp = "default-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://fonts.gstatic.com https://js.stripe.com https://cdn.datatables.net; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://js.stripe.com https://cdn.datatables.net; " +
              "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://cdn.datatables.net; " +
              "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
              "img-src 'self' data: https://via.placeholder.com https://*.stripe.com; " +
              "frame-src https://js.stripe.com; " +
              "connect-src 'self' https://api.stripe.com;";
              
    context.Response.Headers.Append("Content-Security-Policy", csp);
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Home/Error"); 
    // Comentado temporalmente para ver el error real si ocurre en producción
    app.UseHsts();
}

// app.UseStatusCodePagesWithReExecute("/Home/Error/{0}"); // Deshabilitado para depurar 404

var supportedCultures = new[] { "es-MX" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

// app.UseHttpsRedirection(); // Deshabilitado: Coolify maneja SSL externamente
app.UseStaticFiles();

app.UseRouting();

// Enable Rate Limiting Middleware
app.UseRateLimiter();

app.UseSession();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<OrderStatusHub>("/hubs/orderStatus"); // Fase 3: SignalR hub



await SeedDatabase();

app.Run();

async Task SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
    }
}
