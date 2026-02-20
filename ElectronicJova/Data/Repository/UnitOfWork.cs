using ElectronicJova.Models;

namespace ElectronicJova.Data.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;

        public IRepository<Category> Category { get; private set; }
        public IRepository<Product> Product { get; private set; }
        public IRepository<ShoppingCart> ShoppingCart { get; private set; }
        public IRepository<OrderHeader> OrderHeader { get; private set; }
        public IRepository<OrderDetail> OrderDetail { get; private set; }
        public IRepository<OrderStatusLog> OrderStatusLog { get; private set; }
        public IRepository<ApplicationUser> ApplicationUser { get; private set; }
        public IRepository<ProductOption> ProductOption { get; private set; }
        public IRepository<Wishlist> Wishlist { get; private set; }

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            // Initialize each specific repository here
            Category = new Repository<Category>(_db);
            Product = new Repository<Product>(_db);
            ShoppingCart = new Repository<ShoppingCart>(_db);
            OrderHeader = new Repository<OrderHeader>(_db);
            OrderDetail = new Repository<OrderDetail>(_db);
            OrderStatusLog = new Repository<OrderStatusLog>(_db);
            ApplicationUser = new Repository<ApplicationUser>(_db);
            ProductOption = new Repository<ProductOption>(_db);
            Wishlist = new Repository<Wishlist>(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }

        public async System.Threading.Tasks.Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
