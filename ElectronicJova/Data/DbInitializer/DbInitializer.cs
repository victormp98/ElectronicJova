using ElectronicJova.Data;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicJova.Models;

namespace ElectronicJova.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            ILogger<DbInitializer> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            // Migrations if they are not applied
            try
            {
                if ((await _db.Database.GetPendingMigrationsAsync()).Any())
                {
                    await _db.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during DbInitialization migration: {ex.Message}");
            }

            // Create roles if they do not exist
            if (!await _roleManager.RoleExistsAsync(SD.Role_Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
            }

            // Create or update admin user
            var adminUser = await _userManager.FindByEmailAsync("admin@electronicjova.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@electronicjova.com",
                    Email = "admin@electronicjova.com",
                    Name = "Administrador",
                    PhoneNumber = "1112223333",
                    StreetAddress = "123 Admin St",
                    City = "Admin City",
                    State = "AA",
                    PostalCode = "11111",
                    EmailConfirmed = true // CRITICAL: Bypass email confirmation for seed admin
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin123*");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Error al crear admin: {Errors}", errors);
                }
            }
            else
            {
                // Si ya existe pero no tiene nombre, actualizarlo
                bool needsUpdate = false;
                if (string.IsNullOrEmpty(adminUser.Name))
                {
                    adminUser.Name = "Administrador";
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await _userManager.UpdateAsync(adminUser);
                }

                // Asegurar que tenga EmailConfirmed = true (Fix para bloqueo de login)
                if (!adminUser.EmailConfirmed)
                {
                    adminUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(adminUser);
                }

                // Asegurar que tenga el rol Admin
                if (!await _userManager.IsInRoleAsync(adminUser, SD.Role_Admin))
                {
                    await _userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
                }
            }
            // 1. Seed Categories (Electronics)
            var categoryNames = new[] { "Computadoras", "Celulares", "Gaming", "Accesorios", "Audio" };
            for (int i = 0; i < categoryNames.Length; i++)
            {
                var name = categoryNames[i];
                if (!_db.Categories.Any(c => c.Name == name))
                {
                    _db.Categories.Add(new Category { Name = name, DisplayOrder = i + 1 });
                }
            }
            await _db.SaveChangesAsync();

            // 2. Get Category Ids safely
            var catComputadoras = _db.Categories.FirstOrDefault(c => c.Name == "Computadoras")?.Id;
            var catCelulares = _db.Categories.FirstOrDefault(c => c.Name == "Celulares")?.Id;
            var catGaming = _db.Categories.FirstOrDefault(c => c.Name == "Gaming")?.Id;
            var catAccesorios = _db.Categories.FirstOrDefault(c => c.Name == "Accesorios")?.Id;
            var catAudio = _db.Categories.FirstOrDefault(c => c.Name == "Audio")?.Id;

            // 3. Seed Products with safety checks
            var products = new List<Product>();

            if (catComputadoras.HasValue)
            {
                products.Add(new Product { Name = "Laptop Gamer Pro X15", Description = "Potencia extrema con RTX 4080 y procesador i9.", Model = "LAP-001", Brand = "MSI", ListPrice = 2500, Price = 2300, Price50 = 2200, Price100 = 2100, CategoryId = catComputadoras.Value, Stock = 15, ImageUrl = "\\images\\products\\laptop-gamer.png", Specifications = "RTX 4080, 32GB RAM, 1TB SSD", Warranty = "2 años" });
                products.Add(new Product { Name = "Ultrabook Air 13", Description = "Ligera, potente y con batería para todo el día.", Model = "LAP-002", Brand = "Apple", ListPrice = 1200, Price = 1100, Price50 = 1050, Price100 = 1000, CategoryId = catComputadoras.Value, Stock = 20, ImageUrl = "\\images\\products\\macbook.jpg", Specifications = "M2 Chip, 8GB RAM, 256GB SSD", Warranty = "1 año" });
            }

            if (catCelulares.HasValue)
            {
                products.Add(new Product { Name = "iPhone 15 Pro", Description = "Titanio. Tan fuerte. Tan ligero. Tan Pro.", Model = "CEL-001", Brand = "Apple", ListPrice = 999, Price = 950, Price50 = 920, Price100 = 900, CategoryId = catCelulares.Value, Stock = 50, ImageUrl = "\\images\\products\\iphone15.png", Specifications = "A17 Pro chip, 128GB", Warranty = "1 año" });
                products.Add(new Product { Name = "Samsung Galaxy S24", Description = "La IA llega a tu teléfono.", Model = "CEL-002", Brand = "Samsung", ListPrice = 899, Price = 850, Price50 = 820, Price100 = 800, CategoryId = catCelulares.Value, Stock = 40, ImageUrl = "\\images\\products\\s24.jpg", Specifications = "Snapdragon 8 Gen 3, 256GB", Warranty = "1 año" });
            }

            if (catGaming.HasValue)
            {
                products.Add(new Product { Name = "PlayStation 5", Description = "Juega como nunca antes.", Model = "GM-001", Brand = "Sony", ListPrice = 499, Price = 499, Price50 = 480, Price100 = 470, CategoryId = catGaming.Value, Stock = 10, ImageUrl = "\\images\\products\\ps5.jpg", Specifications = "825GB SSD, Digital Edition", Warranty = "1 año" });
                products.Add(new Product { Name = "Xbox Series X", Description = "La Xbox más rápida y potente de la historia.", Model = "GM-002", Brand = "Microsoft", ListPrice = 499, Price = 480, Price50 = 460, Price100 = 450, CategoryId = catGaming.Value, Stock = 12, ImageUrl = "\\images\\products\\xbox.jpg", Specifications = "1TB SSD, True 4K Gaming", Warranty = "1 año" });
            }

            if (catAudio.HasValue)
            {
                products.Add(new Product { Name = "Sony WH-1000XM5", Description = "Cancelación de ruido líder en la industria.", Model = "AUD-001", Brand = "Sony", ListPrice = 350, Price = 320, Price50 = 300, Price100 = 290, CategoryId = catAudio.Value, Stock = 30, ImageUrl = "\\images\\products\\sony-headphones.png", Specifications = "LDAC, 30h batería", Warranty = "1 año" });
            }

            foreach (var prod in products)
            {
                if (!_db.Products.Any(p => p.Model == prod.Model))
                {
                    _db.Products.Add(prod);
                }
            }
            await _db.SaveChangesAsync();
        }
    }

    // Interface for DbInitializer
    public interface IDbInitializer
    {
        Task InitializeAsync();
    }
}
