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
                    PostalCode = "11111"
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

                // Asegurar que tenga el rol Admin
                if (!await _userManager.IsInRoleAsync(adminUser, SD.Role_Admin))
                {
                    await _userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
                }
            }
            // 1. Seed Categories (Electronics)
            var categories = new List<Category>
            {
                new Category { Name = "Computadoras", DisplayOrder = 1 },
                new Category { Name = "Celulares", DisplayOrder = 2 },
                new Category { Name = "Gaming", DisplayOrder = 3 },
                new Category { Name = "Accesorios", DisplayOrder = 4 },
                new Category { Name = "Audio", DisplayOrder = 5 }
            };

            foreach (var cat in categories)
            {
                if (!_db.Categories.Any(c => c.Name == cat.Name))
                {
                    _db.Categories.Add(cat);
                }
            }
            await _db.SaveChangesAsync();

            // 2. Get Category Ids for Products
            var catComputadoras = _db.Categories.FirstOrDefault(c => c.Name == "Computadoras");
            var catCelulares = _db.Categories.FirstOrDefault(c => c.Name == "Celulares");
            var catGaming = _db.Categories.FirstOrDefault(c => c.Name == "Gaming");
            var catAccesorios = _db.Categories.FirstOrDefault(c => c.Name == "Accesorios");
            var catAudio = _db.Categories.FirstOrDefault(c => c.Name == "Audio");

            // 3. Seed Products
            var products = new List<Product>();

            if (catComputadoras != null)
            {
                products.Add(new Product { Title = "Laptop Gamer Pro X15", Description = "Potencia extrema con RTX 4080 y procesador i9.", ISBN = "LAP-001", Author = "MSI", ListPrice = 2500, Price = 2300, Price50 = 2200, Price100 = 2100, CategoryId = catComputadoras.Id, Stock = 15, ImageUrl = "\\images\\products\\laptop-gamer.jpg" });
                products.Add(new Product { Title = "Ultrabook Air 13", Description = "Ligera, potente y con batería para todo el día.", ISBN = "LAP-002", Author = "Apple", ListPrice = 1200, Price = 1100, Price50 = 1050, Price100 = 1000, CategoryId = catComputadoras.Id, Stock = 20, ImageUrl = "\\images\\products\\macbook.jpg" });
            }

            if (catCelulares != null)
            {
                products.Add(new Product { Title = "iPhone 15 Pro", Description = "Titanio. Tan fuerte. Tan ligero. Tan Pro.", ISBN = "CEL-001", Author = "Apple", ListPrice = 999, Price = 950, Price50 = 920, Price100 = 900, CategoryId = catCelulares.Id, Stock = 50, ImageUrl = "\\images\\products\\iphone15.jpg" });
                products.Add(new Product { Title = "Samsung Galaxy S24", Description = "La IA llega a tu teléfono.", ISBN = "CEL-002", Author = "Samsung", ListPrice = 899, Price = 850, Price50 = 820, Price100 = 800, CategoryId = catCelulares.Id, Stock = 40, ImageUrl = "\\images\\products\\s24.jpg" });
            }

            if (catGaming != null)
            {
                products.Add(new Product { Title = "PlayStation 5", Description = "Juega como nunca antes.", ISBN = "GM-001", Author = "Sony", ListPrice = 499, Price = 499, Price50 = 480, Price100 = 470, CategoryId = catGaming.Id, Stock = 10, ImageUrl = "\\images\\products\\ps5.jpg" });
                products.Add(new Product { Title = "Xbox Series X", Description = "La Xbox más rápida y potente de la historia.", ISBN = "GM-002", Author = "Microsoft", ListPrice = 499, Price = 480, Price50 = 460, Price100 = 450, CategoryId = catGaming.Id, Stock = 12, ImageUrl = "\\images\\products\\xbox.jpg" });
            }

            if (catAudio != null)
            {
                products.Add(new Product { Title = "Sony WH-1000XM5", Description = "Cancelación de ruido líder en la industria.", ISBN = "AUD-001", Author = "Sony", ListPrice = 350, Price = 320, Price50 = 300, Price100 = 290, CategoryId = catAudio.Id, Stock = 30, ImageUrl = "\\images\\products\\sony-headphones.jpg" });
            }

            foreach (var prod in products)
            {
                if (!_db.Products.Any(p => p.ISBN == prod.ISBN))
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
