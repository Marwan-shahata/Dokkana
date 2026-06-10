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

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Stores = new GenericRepository<Store>(context);
            Categories = new GenericRepository<Category>(context);
            Products = new GenericRepository<Product>(context);
            CatalogDocuments = new GenericRepository<CatalogDocument>(context);
        }

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
