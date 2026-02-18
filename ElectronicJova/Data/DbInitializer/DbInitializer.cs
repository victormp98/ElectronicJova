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

        public DbInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public void Initialize()
        {
            // Migrations if they are not applied
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during DbInitialization migration: {ex.Message}");
            }

            // Create roles if they do not exist
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();

                // Create admin user
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@electronicjova.com",
                    Email = "admin@electronicjova.com",
                    Name = "Admin User",
                    PhoneNumber = "1112223333",
                    StreetAddress = "123 Admin St",
                    City = "Admin City",
                    State = "AA",
                    PostalCode = "11111"
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser? user = _db.Users.FirstOrDefault(u => u.Email == "admin@electronicjova.com");
                if (user == null)
                {
                    // This scenario should ideally not happen if CreateAsync succeeded, but for robustness
                    // (e.g., if there's a unique constraint violation that CreateAsync silently handles or other issues)
                    // we log an error or handle it. For now, we'll skip adding role if user is null.
                    Console.WriteLine("Error: Admin user not found in DB after creation attempt. Skipping role assignment.");
                }
                else
                {
                    _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
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
        void Initialize();
    }
}
