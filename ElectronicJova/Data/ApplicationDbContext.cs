using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ElectronicJova.Models;

namespace ElectronicJova.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSet properties for the entities based on the database schema
        // The actual model classes (Category, Product, etc.) will be created in T06, T08, T13.
        // For now, these are placeholders for the DbContext configuration.
        public DbSet<Category> Categories { get; set; } = null!; // Model for Categories
        public DbSet<Product> Products { get; set; } = null!; // Model for Products
        public DbSet<ShoppingCart> ShoppingCarts { get; set; } = null!; // Model for ShoppingCarts
        public DbSet<OrderHeader> OrderHeaders { get; set; } = null!; // Model for OrderHeaders
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!; // Model for OrderDetails
        public DbSet<OrderStatusLog> OrderStatusLogs { get; set; } = null!;
        public DbSet<ProductOption> ProductOptions { get; set; } = null!; // Model for ProductOptions
        public DbSet<Wishlist> Wishlists { get; set; } = null!; // Model for Wishlist

        // For now, we will leave OnModelCreating empty.
        // It will be used later for more advanced configuration or seeding.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure decimal precision for monetary values
            modelBuilder.Entity<Product>().Property(p => p.ListPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.Price50).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.Price100).HasPrecision(18, 2);

            modelBuilder.Entity<OrderHeader>().Property(o => o.OrderTotal).HasPrecision(18, 2);
            modelBuilder.Entity<OrderDetail>().Property(od => od.Price).HasPrecision(18, 2);
            modelBuilder.Entity<ProductOption>().Property(po => po.AdditionalPrice).HasPrecision(18, 2);

            // Indexes for performance
            modelBuilder.Entity<Product>().HasIndex(p => p.CategoryId);
            modelBuilder.Entity<ShoppingCart>().HasIndex(sc => sc.ApplicationUserId);
            modelBuilder.Entity<ShoppingCart>().HasIndex(sc => sc.ProductId);
            modelBuilder.Entity<OrderHeader>().HasIndex(o => o.ApplicationUserId);
            modelBuilder.Entity<OrderDetail>().HasIndex(od => od.OrderHeaderId);
            modelBuilder.Entity<OrderDetail>().HasIndex(od => od.ProductId);
            modelBuilder.Entity<OrderStatusLog>().HasIndex(ol => ol.OrderHeaderId);
            modelBuilder.Entity<ProductOption>().HasIndex(po => po.ProductId);
            modelBuilder.Entity<Wishlist>().HasIndex(w => w.ApplicationUserId);
            modelBuilder.Entity<Wishlist>().HasIndex(w => w.ProductId);

            // Configure relationships and delete behavior
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShoppingCart>()
                .HasOne(sc => sc.Product)
                .WithMany()
                .HasForeignKey(sc => sc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.OrderHeader)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderStatusLog>()
                .HasOne(ol => ol.OrderHeader)
                .WithMany(o => o.StatusLogs)
                .HasForeignKey(ol => ol.OrderHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductOption>()
                .HasOne(po => po.Product)
                .WithMany()
                .HasForeignKey(po => po.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.ApplicationUser)
                .WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
