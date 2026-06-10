using Dokkana.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.DAL.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _set;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync() => await _set.AsNoTracking().ToListAsync();

        public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);

        public async Task AddAsync(T entity) => await _set.AddAsync(entity);

        public void Update(T entity) => _set.Update(entity);

        public void Delete(T entity) => _set.Remove(entity);
    }
}
