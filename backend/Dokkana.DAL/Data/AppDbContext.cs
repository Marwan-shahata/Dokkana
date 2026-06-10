using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.DAL.Data
{
    // Relational data lives in SQL Server. DocumentChunk embeddings are kept in the
    // vector store (Supabase pgvector) and are not mapped here.
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<CatalogDocument> CatalogDocuments => Set<CatalogDocument>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);

            // A store's owner; deleting a user should not cascade-delete the store.
            builder.Entity<Store>()
                .HasOne(s => s.Owner)
                .WithOne(u => u.Store)
                .HasForeignKey<Store>(s => s.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
