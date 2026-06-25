using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.API.Seed
{
    // Seeds rich demo data so the monetization features show real numbers on a
    // fresh database: a Pro store with recent sales, a competitor store for price
    // comparison, active + pending campaigns, wallet history, a lead, an alert, a
    // follower, and search trends. Idempotent — a no-op once any Promotion exists.
    public static class DemoSeeder
    {
        public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> users)
        {
            if (await db.Promotions.AnyAsync()) return; // already seeded

            var proStore = await db.Stores
                .Include(s => s.Products)
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync();
            if (proStore is null || proStore.Products.Count == 0) return; // base catalog not ready

            var now = DateTime.UtcNow;

            // 1) Make the primary store a Pro store with WhatsApp + a funded wallet.
            proStore.Tier = StoreTier.Pro;
            proStore.SubscriptionStatus = "Active";
            proStore.SubscriptionRenewsAt = now.AddDays(30);
            proStore.WalletBalance = 5000m;
            proStore.WhatsAppPhone ??= "201000000000";

            // 2) Demo shopper (owns the orders, follow, alert, and lead below).
            var shopper = await users.FindByEmailAsync("shopper@dokkana.com");
            if (shopper is null)
            {
                shopper = new ApplicationUser
                {
                    UserName = "shopper@dokkana.com",
                    Email = "shopper@dokkana.com",
                    FirstName = "Mona",
                    LastName = "Shopper",
                    EmailConfirmed = true
                };
                if ((await users.CreateAsync(shopper, "Shopper123!")).Succeeded)
                    await users.AddToRoleAsync(shopper, "Shopper");
            }

            // 3) Competitor store (Free) priced differently, so MM3 pricing hints fire.
            var rivalOwner = await users.FindByEmailAsync("store2@dokkana.com");
            if (rivalOwner is null)
            {
                rivalOwner = new ApplicationUser
                {
                    UserName = "store2@dokkana.com",
                    Email = "store2@dokkana.com",
                    FirstName = "Cairo",
                    LastName = "Threads",
                    EmailConfirmed = true
                };
                if ((await users.CreateAsync(rivalOwner, "Store123!")).Succeeded)
                    await users.AddToRoleAsync(rivalOwner, "StoreOwner");
            }

            var rival = await db.Stores.FirstOrDefaultAsync(s => s.OwnerId == rivalOwner!.Id);
            if (rival is null)
            {
                rival = new Store
                {
                    Name = "Cairo Threads",
                    Description = "Affordable everyday fashion in Downtown Cairo.",
                    Address = "Downtown, Cairo, Egypt",
                    IsApproved = true,
                    Tier = StoreTier.Free,
                    OwnerId = rivalOwner!.Id
                };
                db.Stores.Add(rival);
                await db.SaveChangesAsync();

                rivalOwner.StoreId = rival.Id;
                await users.UpdateAsync(rivalOwner);

                var cats = await db.Categories.OrderBy(c => c.Id).ToListAsync();
                int? CatId(string n) => cats.FirstOrDefault(c => c.Name == n)?.Id;
                void Add(string cat, string title, decimal price, int count, string color, string gender, string size)
                {
                    var id = CatId(cat);
                    if (id is null) return;
                    db.Products.Add(new Product
                    {
                        Title = title, Price = price, Count = count, Color = color,
                        Gender = gender, Brand = "Local", Size = size,
                        CategoryId = id.Value, StoreId = rival!.Id
                    });
                }
                Add("Dresses", "Everyday Jersey Dress", 230, 18, "Green", "Women", "M");
                Add("Tops", "Basic Cotton Tee", 110, 40, "White", "Unisex", "L");
                Add("Pants", "Slim Chino Pants", 240, 16, "Khaki", "Men", "32");
                Add("Shoes", "Canvas Sneakers", 300, 20, "White", "Unisex", "42");
                Add("Bags", "Canvas Tote", 180, 14, "Beige", "Women", "One Size");
                Add("Outerwear", "Light Bomber Jacket", 520, 9, "Black", "Men", "L");
            }

            await db.SaveChangesAsync();

            // 4) Completed orders over the last ~25 days → feeds MM3 forecast + Copilot + seller stats.
            var picks = proStore.Products.OrderBy(p => p.Id).ToList();
            var rng = new Random(7);
            for (var i = 0; i < 5; i++)
            {
                var order = new Order
                {
                    UserId = shopper!.Id,
                    Status = OrderStatus.Completed,
                    CreatedAt = now.AddDays(-(2 + i * 5)),
                    PickupCode = $"DKN-DEMO{i:00}"
                };
                decimal total = 0;
                var lineCount = 2 + rng.Next(0, 2);
                for (var j = 0; j < lineCount; j++)
                {
                    var p = picks[(i * 3 + j) % picks.Count];
                    var qty = 1 + rng.Next(0, 4);
                    order.Items.Add(new OrderItem
                    {
                        ProductId = p.Id,
                        StoreId = proStore.Id,
                        ProductTitle = p.Title,
                        ImageUrl = p.ImageUrl,
                        StoreName = proStore.Name,
                        UnitPrice = p.Price,
                        Quantity = qty
                    });
                    total += p.Price * qty;
                }
                order.TotalAmount = total;
                db.Orders.Add(order);
            }

            // 5) Campaigns: an active featured product (shows in the sponsored rail +
            //    has stats), an approved sponsored push, and one pending campaign so
            //    the admin approval queue has something to act on.
            var heroProduct = picks[0];
            var secondProduct = picks.Count > 1 ? picks[1] : picks[0];
            db.Promotions.AddRange(
                new Promotion
                {
                    StoreId = proStore.Id,
                    Type = PromotionType.FeaturedProduct,
                    ProductId = heroProduct.Id,
                    Audience = PromotionAudience.AllShoppers,
                    Budget = 500m,
                    Status = PromotionStatus.Active,
                    Impressions = 240,
                    Clicks = 18,
                    CreatedAt = now.AddDays(-6),
                    ApprovedAt = now.AddDays(-6),
                    StartAt = now.AddDays(-6),
                    EndAt = now.AddDays(24)
                },
                new Promotion
                {
                    StoreId = proStore.Id,
                    Type = PromotionType.SponsoredNotification,
                    Title = "New arrivals just dropped",
                    Message = $"Fresh styles landed at {proStore.Name} — tap to explore.",
                    Audience = PromotionAudience.Followers,
                    Budget = 300m,
                    Status = PromotionStatus.Approved,
                    RecipientsCount = 1,
                    Impressions = 1,
                    CreatedAt = now.AddDays(-3),
                    ApprovedAt = now.AddDays(-3)
                },
                new Promotion
                {
                    StoreId = proStore.Id,
                    Type = PromotionType.FeaturedProduct,
                    ProductId = secondProduct.Id,
                    Audience = PromotionAudience.AllShoppers,
                    Budget = 250m,
                    Status = PromotionStatus.Pending,
                    CreatedAt = now.AddHours(-6)
                });

            // 6) Wallet history (top-up + a campaign charge).
            db.WalletTransactions.AddRange(
                new WalletTransaction { StoreId = proStore.Id, Amount = 5500m, Type = "TopUp", Note = "Demo top-up", CreatedAt = now.AddDays(-7) },
                new WalletTransaction { StoreId = proStore.Id, Amount = -500m, Type = "Campaign", Note = $"Featured: {heroProduct.Title}", CreatedAt = now.AddDays(-6) });

            // 7) Engagement: a follower, a saved alert, a WhatsApp lead, and trends.
            if (shopper is not null)
            {
                db.StoreFollows.Add(new StoreFollow { UserId = shopper.Id, StoreId = proStore.Id, CreatedAt = now.AddDays(-10) });
                db.ShopperAlerts.Add(new ShopperAlert
                {
                    UserId = shopper.Id,
                    ProductId = heroProduct.Id,
                    Type = ShopperAlertType.PriceDrop,
                    TargetPrice = heroProduct.Price - 50,
                    IsActive = true,
                    CreatedAt = now.AddDays(-4)
                });
            }

            db.MerchantLeads.Add(new MerchantLead
            {
                StoreId = proStore.Id,
                ProductId = heroProduct.Id,
                UserId = shopper?.Id,
                Type = "WhatsApp",
                CustomerName = shopper is null ? null : $"{shopper.FirstName} {shopper.LastName}",
                CustomerEmail = shopper?.Email,
                CreatedAt = now.AddDays(-1)
            });

            var trends = new (string q, int c)[]
            {
                ("white sneakers", 42), ("red dress", 35), ("leather jacket", 28),
                ("كوتشي ابيض", 24), ("فستان سواريه", 19), ("tote bag", 12)
            };
            foreach (var (q, c) in trends)
                db.SearchTrends.Add(new SearchTrend { Query = q, NormalizedQuery = q.ToLowerInvariant(), Count = c, LastSearchedAt = now.AddDays(-1) });

            await db.SaveChangesAsync();
        }
    }
}
