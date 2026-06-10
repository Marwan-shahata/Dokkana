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

        Task<int> SaveChangesAsync();
    }
}
