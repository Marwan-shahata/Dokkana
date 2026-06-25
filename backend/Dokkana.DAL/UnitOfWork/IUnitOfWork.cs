using Dokkana.DAL.Models;
using Dokkana.DAL.Repositories;

namespace Dokkana.DAL.UnitOfWork
{
    // Aggregates repositories and commits them in a single transaction.
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Store> Stores { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<CatalogDocument> CatalogDocuments { get; }
        IGenericRepository<Cart> Carts { get; }
        IGenericRepository<CartItem> CartItems { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<OrderItem> OrderItems { get; }
        IGenericRepository<FavoriteProduct> FavoriteProducts { get; }
        IGenericRepository<StoreFollow> StoreFollows { get; }
        IGenericRepository<ProductReview> ProductReviews { get; }
        IGenericRepository<AuditLog> AuditLogs { get; }

        Task<int> SaveChangesAsync();
    }
}
