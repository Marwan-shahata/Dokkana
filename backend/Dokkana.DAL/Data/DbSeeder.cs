using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.DAL.Data;

public sealed class DbSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public DbSeeder(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        if (await _context.Products.AnyAsync())
            return;

        await EnsureRolesAsync();
        await SeedUsersAsync();
        var stores = await SeedStoresAsync();
        var categories = await SeedCategoriesAsync();
        await SeedProductsAsync(stores, categories);
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in new[] { "Admin", "StoreOwner", "Shopper" })
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new ApplicationRole(role));
        }
    }

    private async Task SeedUsersAsync()
    {
        var demoOwners = new List<ApplicationUser>
        {
            new()
            {
                Id = "demo-owner-1",
                UserName = "cairo_fashion_owner",
                Email = "owner1@dokkana.com",
                FirstName = "Ahmed",
                LastName = "Ali"
            },
            new()
            {
                Id = "demo-owner-2",
                UserName = "alex_style_owner",
                Email = "owner2@dokkana.com",
                FirstName = "Mona",
                LastName = "Hassan"
            },
            new()
            {
                Id = "demo-owner-3",
                UserName = "modern_wear_owner",
                Email = "owner3@dokkana.com",
                FirstName = "Omar",
                LastName = "Youssef"
            }
        };

        foreach (var user in demoOwners)
        {
            var existing = await _userManager.FindByIdAsync(user.Id);
            if (existing is not null)
                continue;

            var result = await _userManager.CreateAsync(user, "Demo@123");
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, "StoreOwner");
        }
    }

    private async Task<List<Store>> SeedStoresAsync()
    {
        var stores = new List<Store>
        {
            new()
            {
                Name = "Cairo Fashion - موضة القاهرة",
                Description = "Premium men's and women's fashion in the heart of Cairo / أزياء راقية للرجال والنساء في وسط القاهرة",
                Address = "Downtown Cairo, Tahrir Square",
                IsApproved = true,
                OwnerId = "demo-owner-1"
            },
            new()
            {
                Name = "Alexandria Style - إسكندرية ستايل",
                Description = "Trendy Mediterranean fashion for women / أزياء البحر المتوسط العصرية للنساء",
                Address = "Alexandria Corniche, Stanly",
                IsApproved = true,
                OwnerId = "demo-owner-2"
            },
            new()
            {
                Name = "Modern Wear - الأزياء العصرية",
                Description = "Contemporary fashion for all ages and genders / أزياء عصرية لجميع الأعمار",
                Address = "New Cairo, Fifth Settlement",
                IsApproved = true,
                OwnerId = "demo-owner-3"
            }
        };

        _context.Stores.AddRange(stores);
        await _context.SaveChangesAsync();
        return stores;
    }

    private async Task<List<Category>> SeedCategoriesAsync()
    {
        var categories = new List<Category>
        {
            new() { Name = "T-Shirts - تيشيرتات", Description = "Casual t-shirts and tops / تيشيرتات كاجوال" },
            new() { Name = "Shirts - قمصان", Description = "Formal and casual shirts and outerwear / قمصان رسمية وكاجوال" },
            new() { Name = "Dresses - فساتين", Description = "Dresses for every occasion / فساتين لكل المناسبات" },
            new() { Name = "Pants & Jeans - بناطيل وجينز", Description = "Bottoms, denim and shorts / بناطيل وجينز وشورت" },
            new() { Name = "Shoes & Accessories - أحذية وإكسسوارات", Description = "Footwear, bags and fashion accessories / أحذية وحقائب وإكسسوارات" }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();
        return categories;
    }

    private async Task SeedProductsAsync(List<Store> stores, List<Category> categories)
    {
        var products = new List<Product>
        {
            // ── Store 1: Cairo Fashion ──────────────────────────────────────
            new()
            {
                Title = "Black Cotton T-Shirt - تيشيرت قطن أسود",
                Price = 350m,
                Count = 100,
                Color = "Black",
                Size = "M, L, XL",
                Brand = "Dokkana Wear",
                Gender = "Men",
                CategoryId = categories[0].Id,  // T-Shirts
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/black-tshirt.jpg"
            },
            new()
            {
                Title = "White Cotton T-Shirt - تيشيرت قطن أبيض",
                Price = 350m,
                Count = 80,
                Color = "White",
                Size = "M, L, XL",
                Brand = "Dokkana Wear",
                Gender = "Men",
                CategoryId = categories[0].Id,
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/white-tshirt.jpg"
            },
            new()
            {
                Title = "Red T-Shirt - تيشيرت أحمر",
                Price = 300m,
                Count = 60,
                Color = "Red",
                Size = "S, M, L",
                Brand = "Dokkana Wear",
                Gender = "Women",
                CategoryId = categories[0].Id,
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/red-tshirt.jpg"
            },
            new()
            {
                Title = "White Formal Shirt - قميص رسمي أبيض",
                Price = 400m,
                Count = 50,
                Color = "White",
                Size = "M, L, XL",
                Brand = "Elegance",
                Gender = "Men",
                CategoryId = categories[1].Id,  // Shirts
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/white-shirt.jpg"
            },
            new()
            {
                Title = "Cotton Shirt - قميص قطن",
                Price = 380m,
                Count = 45,
                Color = "Light Blue",
                Size = "M, L, XL",
                Brand = "Elegance",
                Gender = "Men",
                CategoryId = categories[1].Id,
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/cotton-shirt.jpg"
            },
            new()
            {
                Title = "Blue Jeans - جينز أزرق",
                Price = 600m,
                Count = 70,
                Color = "Blue",
                Size = "32, 34, 36",
                Brand = "Denim Co.",
                Gender = "Men",
                CategoryId = categories[3].Id,  // Pants & Jeans
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/blue-jeans.jpg"
            },
            new()
            {
                Title = "Black Skinny Jeans - جينز أسود نحيف",
                Price = 550m,
                Count = 55,
                Color = "Black",
                Size = "30, 32, 34",
                Brand = "Denim Co.",
                Gender = "Men",
                CategoryId = categories[3].Id,
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/black-skinny-jeans.jpg"
            },
            new()
            {
                Title = "Summer Shorts - شورت صيفي",
                Price = 280m,
                Count = 90,
                Color = "Beige",
                Size = "M, L, XL",
                Brand = "Dokkana Wear",
                Gender = "Men",
                CategoryId = categories[3].Id,
                StoreId = stores[0].Id,
                ImageUrl = "/images/products/summer-shorts.jpg"
            },

            // ── Store 2: Alexandria Style ──────────────────────────────────
            new()
            {
                Title = "Red Summer Dress - فستان صيفي أحمر",
                Price = 480m,
                Count = 40,
                Color = "Red",
                Size = "S, M, L",
                Brand = "Med Style",
                Gender = "Women",
                CategoryId = categories[2].Id,  // Dresses
                StoreId = stores[1].Id,
                ImageUrl = "/images/products/red-dress.jpg"
            },
            new()
            {
                Title = "Blue Floral Dress - فستان أزرق منقوش",
                Price = 550m,
                Count = 35,
                Color = "Blue",
                Size = "S, M, L",
                Brand = "Med Style",
                Gender = "Women",
                CategoryId = categories[2].Id,
                StoreId = stores[1].Id,
                ImageUrl = "/images/products/blue-floral-dress.jpg"
            },
            new()
            {
                Title = "Elegant Evening Dress - فستان سهرة أنيق",
                Price = 1500m,
                Count = 15,
                Color = "Black",
                Size = "S, M, L",
                Brand = "Med Style",
                Gender = "Women",
                CategoryId = categories[2].Id,
                StoreId = stores[1].Id,
                ImageUrl = "/images/products/evening-dress.jpg"
            },
            new()
            {
                Title = "Women Leather Bag - حقيبة جلدية نسائية",
                Price = 850m,
                Count = 30,
                Color = "Brown",
                Size = "One Size",
                Brand = "Accessori",
                Gender = "Women",
                CategoryId = categories[4].Id,  // Shoes & Accessories
                StoreId = stores[1].Id,
                ImageUrl = "/images/products/leather-bag.jpg"
            },
            new()
            {
                Title = "Beige Scarf - وشاح بيج",
                Price = 200m,
                Count = 65,
                Color = "Beige",
                Size = "One Size",
                Brand = "Accessori",
                Gender = "Women",
                CategoryId = categories[4].Id,
                StoreId = stores[1].Id,
                ImageUrl = "/images/products/beige-scarf.jpg"
            },

            // ── Store 3: Modern Wear ──────────────────────────────────────
            new()
            {
                Title = "Beige Hoodie - هودي بيج",
                Price = 450m,
                Count = 50,
                Color = "Beige",
                Size = "M, L, XL",
                Brand = "Urban",
                Gender = "Unisex",
                CategoryId = categories[0].Id,  // T-Shirts
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/beige-hoodie.jpg"
            },
            new()
            {
                Title = "Black Leather Shoes - حذاء جلدي أسود",
                Price = 750m,
                Count = 40,
                Color = "Black",
                Size = "40, 41, 42, 43",
                Brand = "Step Up",
                Gender = "Men",
                CategoryId = categories[4].Id,
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/black-shoes.jpg"
            },
            new()
            {
                Title = "Kids Sneakers - حذاء رياضي أطفال",
                Price = 350m,
                Count = 60,
                Color = "Blue",
                Size = "28, 30, 32",
                Brand = "Step Up",
                Gender = "Kids",
                CategoryId = categories[4].Id,
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/kids-sneakers.jpg"
            },
            new()
            {
                Title = "White Sneakers - حذاء رياضي أبيض",
                Price = 500m,
                Count = 55,
                Color = "White",
                Size = "40, 41, 42, 43",
                Brand = "Step Up",
                Gender = "Unisex",
                CategoryId = categories[4].Id,
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/white-sneakers.jpg"
            },
            new()
            {
                Title = "Men Winter Jacket - جاكت شتوي رجالي",
                Price = 1200m,
                Count = 25,
                Color = "Black",
                Size = "M, L, XL",
                Brand = "Urban",
                Gender = "Men",
                CategoryId = categories[1].Id,  // Shirts
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/men-jacket.jpg"
            },
            new()
            {
                Title = "Denim Jacket - جاكت جينز",
                Price = 900m,
                Count = 30,
                Color = "Blue",
                Size = "M, L, XL",
                Brand = "Denim Co.",
                Gender = "Unisex",
                CategoryId = categories[1].Id,
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/denim-jacket.jpg"
            },
            new()
            {
                Title = "Kids Blue T-Shirt - تيشيرت أطفال أزرق",
                Price = 200m,
                Count = 80,
                Color = "Blue",
                Size = "4, 6, 8",
                Brand = "Urban",
                Gender = "Kids",
                CategoryId = categories[0].Id,
                StoreId = stores[2].Id,
                ImageUrl = "/images/products/kids-tshirt.jpg"
            }
        };

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }
}
