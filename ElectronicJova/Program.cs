using ElectronicJova.Data;
using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using Microsoft.EntityFrameworkCore;
using ElectronicJova.DbInitializer;
using Pomelo.EntityFrameworkCore.MySql; // Added for UseMySql and MySqlServerVersion
using Microsoft.AspNetCore.Identity; // Added for IdentityRole (explicitly)
using Serilog;
using Microsoft.Extensions.Configuration;
using ElectronicJova.Utilities; // Added for IEmailSender and ResendEmailSender
using Stripe; // Added for StripeConfiguration

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build())
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day) // Add file sink
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// Configure DbContext for MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 0)))
);

// Register UnitOfWork and IRepository
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
// You can customize Identity options here, for example:
// .AddDefaultTokenProviders();

// Register Email Sender
builder.Services.AddSingleton<IEmailSender>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["ResendSettings:ApiKey"];
    var senderEmail = configuration["ResendSettings:SenderEmail"];
    var senderName = configuration["ResendSettings:SenderName"];
    return new ResendEmailSender(apiKey, senderEmail, senderName);
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
StripeConfiguration.ApiKey = builder.Configuration["StripeSettings:SecretKey"];

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Must be before UseAuthorization
app.UseSerilogRequestLogging(); // Add Serilog request logging
app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "Areas",
    pattern: "{area}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
SeedDatabase();

app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}
