using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Dokkana.DAL.Repositories;

namespace Dokkana.DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IGenericRepository<Store> Stores { get; }
        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Product> Products { get; }
        public IGenericRepository<CatalogDocument> CatalogDocuments { get; }
        public IGenericRepository<Cart> Carts { get; }
        public IGenericRepository<CartItem> CartItems { get; }
        public IGenericRepository<Order> Orders { get; }
        public IGenericRepository<OrderItem> OrderItems { get; }
        public IGenericRepository<FavoriteProduct> FavoriteProducts { get; }
        public IGenericRepository<StoreFollow> StoreFollows { get; }
        public IGenericRepository<ProductReview> ProductReviews { get; }
        public IGenericRepository<AuditLog> AuditLogs { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Stores = new GenericRepository<Store>(context);
            Categories = new GenericRepository<Category>(context);
            Products = new GenericRepository<Product>(context);
            CatalogDocuments = new GenericRepository<CatalogDocument>(context);
            Carts = new GenericRepository<Cart>(context);
            CartItems = new GenericRepository<CartItem>(context);
            Orders = new GenericRepository<Order>(context);
            OrderItems = new GenericRepository<OrderItem>(context);
            FavoriteProducts = new GenericRepository<FavoriteProduct>(context);
            StoreFollows = new GenericRepository<StoreFollow>(context);
            ProductReviews = new GenericRepository<ProductReview>(context);
            AuditLogs = new GenericRepository<AuditLog>(context);
        }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
