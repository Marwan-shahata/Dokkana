using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.DAL.Data
{
    // Relational data and embedding JSON live in SQL Server.
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<CatalogDocument> CatalogDocuments => Set<CatalogDocument>();
        // Catalog RAG: extracted PDF chunks + their embeddings (stored as JSON).
        public DbSet<DocumentChunk> CatalogChunks => Set<DocumentChunk>();
        // Product RAG: one embedding row per product for semantic catalog search.
        public DbSet<ProductEmbedding> ProductEmbeddings => Set<ProductEmbedding>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<FavoriteProduct> FavoriteProducts => Set<FavoriteProduct>();
        public DbSet<StoreFollow> StoreFollows => Set<StoreFollow>();
        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<ShopperAlert> ShopperAlerts => Set<ShopperAlert>();
        public DbSet<MerchantLead> MerchantLeads => Set<MerchantLead>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<SearchTrend> SearchTrends => Set<SearchTrend>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.OriginalPrice).HasPrecision(18, 2);
            builder.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 2);

            // A store's owner; deleting a user should not cascade-delete the store.
            builder.Entity<Store>()
                .HasOne(s => s.Owner)
                .WithOne(u => u.Store)
                .HasForeignKey<Store>(s => s.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<Store>().Property(s => s.WalletBalance).HasPrecision(18, 2);

            // Cart
            // One cart per user; deleting the user clears their cart.
            builder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Cart>().HasIndex(c => c.UserId).IsUnique();

            // Cart owns its items; a product in a cart cannot be cascade-deleted
            // (avoids multiple cascade paths to CartItem).
            builder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order
            builder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order owns its items; the product link is optional (SetNull) so a
            // later product deletion keeps the snapshotted order line intact.
            builder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            // Marketplace engagement
            builder.Entity<FavoriteProduct>()
                .HasIndex(f => new { f.UserId, f.ProductId })
                .IsUnique();
            builder.Entity<FavoriteProduct>()
                .HasOne(f => f.User)
                .WithMany(u => u.FavoriteProducts)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<FavoriteProduct>()
                .HasOne(f => f.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StoreFollow>()
                .HasIndex(f => new { f.UserId, f.StoreId })
                .IsUnique();
            builder.Entity<StoreFollow>()
                .HasOne(f => f.User)
                .WithMany(u => u.FollowedStores)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<StoreFollow>()
                .HasOne(f => f.Store)
                .WithMany(s => s.Followers)
                .HasForeignKey(f => f.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductReview>()
                .HasIndex(r => new { r.UserId, r.ProductId })
                .IsUnique();
            builder.Entity<ProductReview>()
                .HasOne(r => r.User)
                .WithMany(u => u.ProductReviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProductReview>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AuditLog>()
                .HasOne(a => a.Actor)
                .WithMany()
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Entity<AuditLog>().HasIndex(a => a.CreatedAt);

            // Notifications: belong to a user; deleting the user removes them.
            // Indexed by (UserId, CreatedAt) for the "my latest notifications" query.
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.CreatedAt });

            // Promotions (paid featured listings + sponsored notifications).
            // Deleting a store removes its promotions; a deleted product detaches.
            builder.Entity<Promotion>(entity =>
            {
                entity.Property(p => p.Budget).HasPrecision(18, 2);
                entity.HasIndex(p => p.StoreId);
                entity.HasIndex(p => p.Status);
                // NoAction (not Cascade) on the Store FK: Products also cascade from
                // Stores, so a Store->Promotion cascade would create a second cascade
                // path to Promotions (SQL Server rejects this).
                entity.HasOne(p => p.Store)
                    .WithMany()
                    .HasForeignKey(p => p.StoreId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(p => p.Product)
                    .WithMany()
                    .HasForeignKey(p => p.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ShopperAlert>(entity =>
            {
                entity.Property(a => a.TargetPrice).HasPrecision(18, 2);
                entity.HasIndex(a => new { a.UserId, a.IsActive });
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Product)
                    .WithMany()
                    .HasForeignKey(a => a.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<MerchantLead>(entity =>
            {
                entity.HasIndex(l => new { l.StoreId, l.CreatedAt });
                // NoAction on the Store FK for the same multiple-cascade-path reason
                // as Promotions (Products already cascade from Stores).
                entity.HasOne(l => l.Store)
                    .WithMany()
                    .HasForeignKey(l => l.StoreId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(l => l.Product)
                    .WithMany()
                    .HasForeignKey(l => l.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(l => l.User)
                    .WithMany()
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<WalletTransaction>(entity =>
            {
                entity.Property(t => t.Amount).HasPrecision(18, 2);
                entity.HasIndex(t => new { t.StoreId, t.CreatedAt });
                entity.HasOne(t => t.Store)
                    .WithMany()
                    .HasForeignKey(t => t.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SearchTrend>(entity =>
            {
                entity.HasIndex(t => t.NormalizedQuery).IsUnique();
            });

            // Catalog RAG: a document owns its chunks; deleting the document
            // removes its chunks. Embeddings are kept as JSON text (nvarchar(max)).
            builder.Entity<CatalogDocument>(entity =>
            {
                entity.Property(x => x.FileName).HasMaxLength(300);
                entity.HasMany(x => x.Chunks)
                    .WithOne(x => x.CatalogDocument)
                    .HasForeignKey(x => x.CatalogDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DocumentChunk>(entity =>
            {
                entity.Property(x => x.Content).IsRequired();
                entity.Property(x => x.EmbeddingJson).HasColumnType("nvarchar(max)");
                entity.Property(x => x.EmbeddingModel).HasMaxLength(200);
                entity.HasIndex(x => x.StoreId);
                entity.HasIndex(x => x.CatalogDocumentId);
            });

            // Product RAG: one embedding per product; removed with the product.
            builder.Entity<ProductEmbedding>(entity =>
            {
                entity.Property(x => x.EmbeddingJson).HasColumnType("nvarchar(max)");
                entity.Property(x => x.EmbeddingModel).HasMaxLength(200);
                entity.HasIndex(x => x.ProductId).IsUnique();
                entity.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
