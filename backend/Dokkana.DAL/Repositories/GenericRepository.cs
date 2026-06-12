using Dokkana.DAL.Data;
using System.Linq.Expressions;
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

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => await _set.AsNoTracking().Where(predicate).ToListAsync();

        public async Task<int> CountAsync() => await _set.CountAsync();

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
            => await _set.CountAsync(predicate);

        /// <summary>
        /// Returns an IQueryable for complex queries (filtering, paging, includes).
        /// Callers are responsible for materializing with ToListAsync().
        /// </summary>
        public IQueryable<T> Query() => _set.AsNoTracking();

        public async Task AddAsync(T entity) => await _set.AddAsync(entity);

        public void Update(T entity) => _set.Update(entity);

        public void Delete(T entity) => _set.Remove(entity);
    }
}

