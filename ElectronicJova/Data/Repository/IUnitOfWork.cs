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
        // Potentially an IRepository for ApplicationUser if we need custom operations beyond Identity defaults

        void Save();
        System.Threading.Tasks.Task SaveAsync();
    }
}
