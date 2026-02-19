
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
using CloudinaryDotNet;

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build())
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
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOptions();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["ResendSettings:ApiKey"] ?? string.Empty;
});
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, ResendEmailSender>();

// Configure Cloudinary
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddSingleton<Cloudinary>(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudinarySettings>>().Value;
    if (config == null) throw new InvalidOperationException("Cloudinary settings are missing.");
    var account = new CloudinaryDotNet.Account(config.CloudName, config.ApiKey, config.ApiSecret);
    return new Cloudinary(account);
});


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
if (string.IsNullOrEmpty(stripeSecretKey))
    throw new InvalidOperationException("StripeSettings:SecretKey no está configurado. Revisa appsettings.json o las variables de entorno.");
if (string.IsNullOrEmpty(stripeWebhookSecret))
    throw new InvalidOperationException("StripeSettings:WebhookSecret no está configurado. Sin esto, los webhooks de Stripe no se verifican.");
StripeConfiguration.ApiKey = stripeSecretKey;


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
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
