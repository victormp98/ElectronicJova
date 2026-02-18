using ElectronicJova.Models;

namespace ElectronicJova.Data.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        // Properties for each specific repository
        // These will be initialized in the concrete UnitOfWork class
        IRepository<Category> Category { get; }
        IRepository<Product> Product { get; }
        IRepository<ShoppingCart> ShoppingCart { get; }
        IRepository<OrderHeader> OrderHeader { get; }
        IRepository<OrderDetail> OrderDetail { get; }
        IRepository<ApplicationUser> ApplicationUser { get; }
        IRepository<ProductOption> ProductOption { get; }
        IRepository<Wishlist> Wishlist { get; }

        void Save();
        System.Threading.Tasks.Task SaveAsync();
    }
}
