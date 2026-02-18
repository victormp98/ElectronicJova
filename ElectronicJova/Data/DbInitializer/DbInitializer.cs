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
            // Seed Categories if they do not exist
            if (!_db.Categories.Any())
            {
                _db.Categories.AddRange(
                    new Category { Name = "Action", DisplayOrder = 1 },
                    new Category { Name = "SciFi", DisplayOrder = 2 },
                    new Category { Name = "History", DisplayOrder = 3 }
                );
                _db.SaveChanges();
            }

            // Seed Products if they do not exist
            if (!_db.Products.Any())
            {
                // Assuming Categories have been seeded (Id 1, 2, 3)
                _db.Products.AddRange(
                    new Product
                    {
                        Title = "The Lord of the Rings",
                        Description = "Fantasy novel series by J. R. R. Tolkien.",
                        ISBN = "978-0618260274",
                        Author = "J. R. R. Tolkien",
                        ListPrice = 30.00M,
                        Price = 27.00M,
                        Price50 = 25.00M,
                        Price100 = 22.00M,
                        CategoryId = 1, // Action
                        ImageUrl = "",
                        Stock = 10
                    },
                    new Product
                    {
                        Title = "Dune",
                        Description = "Science fiction novel by Frank Herbert.",
                        ISBN = "978-0441172719",
                        Author = "Frank Herbert",
                        ListPrice = 25.00M,
                        Price = 22.50M,
                        Price50 = 20.00M,
                        Price100 = 18.00M,
                        CategoryId = 2, // SciFi
                        ImageUrl = "",
                        Stock = 5
                    }
                );
                _db.SaveChanges();

                // Seed Product Options for LOTR (assuming Id 1)
                var lotr = _db.Products.FirstOrDefault(p => p.Title == "The Lord of the Rings");
                if (lotr != null)
                {
                    _db.ProductOptions.AddRange(
                        new ProductOption { ProductId = lotr.Id, Name = "Garantía", Value = "1 año extra", AdditionalPrice = 10.00M, DisplayOrder = 1 },
                        new ProductOption { ProductId = lotr.Id, Name = "Edición", Value = "Coleccionista", AdditionalPrice = 25.00M, DisplayOrder = 2 },
                        new ProductOption { ProductId = lotr.Id, Name = "Soporte", Value = "Digital", AdditionalPrice = 0.00M, DisplayOrder = 3 }
                    );
                    _db.SaveChanges();
                }
            }
        }
    }

    // Interface for DbInitializer
    public interface IDbInitializer
    {
        Task InitializeAsync();
    }
}
