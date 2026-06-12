using System.Text;
using Dokkana.BLL.AI.Agent;
using Dokkana.BLL.Managers;
using Dokkana.BLL.Services;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Dokkana.API.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers + OpenAPI ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── CORS (allow the Angular dev server) ──────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Database (SQL Server) ────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── JWT authentication ───────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "CHANGE_ME_DEV_ONLY_SECRET_KEY_32CHARS_MIN";
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("StoreOwnerOnly", p => p.RequireRole("StoreOwner"));
    options.AddPolicy("ShopperOnly", p => p.RequireRole("Shopper"));
});

// ── Dependency Injection: DAL + BLL ──────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStoreManager, StoreManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProductManager, ProductManager>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IDokkanaAgent, DokkanaAgent>();
builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

// ── Seed roles (Admin / StoreOwner / Shopper) ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    foreach (var role in new[] { "Admin", "StoreOwner", "Shopper" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole(role));
    }

    // ── Seed default categories ──────────────────────────────────────────────
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Categories.Any())
    {
        var categories = new[]
        {
            "Dresses", "Tops", "Pants", "Shoes",
            "Bags", "Accessories", "Outerwear", "Activewear"
        };

        foreach (var name in categories)
            db.Categories.Add(new Dokkana.DAL.Models.Category { Name = name });

        await db.SaveChangesAsync();
    }

    // ── Seed admin user ──────────────────────────────────────────────────────
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["Seed:AdminEmail"] ?? "admin@dokkana.com";
    var adminPassword = app.Configuration["Seed:AdminPassword"] ?? "Admin123!";

    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "Dokkana",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }

    // ── Seed sample store + store-owner user ──────────────────────────────────
    if (!db.Stores.Any())
    {
        var storeOwner = await userManager.FindByEmailAsync("store@dokkana.com");
        if (storeOwner is null)
        {
            storeOwner = new ApplicationUser
            {
                UserName = "store@dokkana.com",
                Email = "store@dokkana.com",
                FirstName = "Sample",
                LastName = "Store",
                EmailConfirmed = true
            };
            var soResult = await userManager.CreateAsync(storeOwner, "Store123!");
            if (soResult.Succeeded)
                await userManager.AddToRoleAsync(storeOwner, "StoreOwner");
        }

        db.Stores.Add(new Dokkana.DAL.Models.Store
        {
            Name = "Dokkana Sample Store",
            Description = "A curated fashion boutique featuring trending styles.",
            Address = "Cairo, Egypt",
            IsApproved = true,
            OwnerId = storeOwner.Id
        });
        await db.SaveChangesAsync();
    }

    // ── Seed products (12 Men + 12 Women, 3 per category) ─────────────────────
    if (!db.Products.Any())
    {
        var cats = await db.Categories.ToListAsync();
        var storeId = (await db.Stores.FirstAsync()).Id;

        var seedProducts = new List<Dokkana.DAL.Models.Product>();

        // ── Dresses ── (3 Women)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Floral Maxi Dress", Price = 450, Count = 15, Color = "Red", Size = "M", Gender = "Women", Brand = "Zara", CategoryId = cats[0].Id, StoreId = storeId },
            new Product { Title = "Little Black Dress", Price = 320, Count = 20, Color = "Black", Size = "S", Gender = "Women", Brand = "H&M", CategoryId = cats[0].Id, StoreId = storeId },
            new Product { Title = "Summer Sundress", Price = 250, Count = 12, Color = "Yellow", Size = "L", Gender = "Women", Brand = "Mango", CategoryId = cats[0].Id, StoreId = storeId },
        });

        // ── Tops ── (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "White Cotton Blouse", Price = 180, Count = 25, Color = "White", Size = "M", Gender = "Women", Brand = "Zara", CategoryId = cats[1].Id, StoreId = storeId },
            new Product { Title = "Striped Linen Shirt", Price = 220, Count = 18, Color = "Blue", Size = "L", Gender = "Men", Brand = "Massimo Dutti", CategoryId = cats[1].Id, StoreId = storeId },
            new Product { Title = "Casual Denim Jacket Top", Price = 290, Count = 10, Color = "Denim", Size = "L", Gender = "Men", Brand = "Levi's", CategoryId = cats[1].Id, StoreId = storeId },
        });

        // ── Pants ── (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Tailored Trousers", Price = 350, Count = 14, Color = "Navy", Size = "32", Gender = "Men", Brand = "Massimo Dutti", CategoryId = cats[2].Id, StoreId = storeId },
            new Product { Title = "High-Waisted Jeans", Price = 280, Count = 22, Color = "Blue", Size = "30", Gender = "Women", Brand = "Levi's", CategoryId = cats[2].Id, StoreId = storeId },
            new Product { Title = "Wide-Leg Linen Pants", Price = 310, Count = 8, Color = "Beige", Size = "M", Gender = "Women", Brand = "Mango", CategoryId = cats[2].Id, StoreId = storeId },
        });

        // ── Shoes ── (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Leather Loafers", Price = 520, Count = 10, Color = "Brown", Size = "42", Gender = "Men", Brand = "Clarks", CategoryId = cats[3].Id, StoreId = storeId },
            new Product { Title = "White Sneakers", Price = 380, Count = 30, Color = "White", Size = "42", Gender = "Men", Brand = "Nike", CategoryId = cats[3].Id, StoreId = storeId },
            new Product { Title = "Elegant Heels", Price = 490, Count = 7, Color = "Black", Size = "38", Gender = "Women", Brand = "Zara", CategoryId = cats[3].Id, StoreId = storeId },
        });

        // ── Bags ── (2 Women, 1 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Tote Bag", Price = 420, Count = 9, Color = "Tan", Size = "One Size", Gender = "Women", Brand = "Zara", CategoryId = cats[4].Id, StoreId = storeId },
            new Product { Title = "Crossbody Bag", Price = 360, Count = 11, Color = "Black", Size = "One Size", Gender = "Women", Brand = "Mango", CategoryId = cats[4].Id, StoreId = storeId },
            new Product { Title = "Backpack", Price = 290, Count = 13, Color = "Gray", Size = "One Size", Gender = "Men", Brand = "H&M", CategoryId = cats[4].Id, StoreId = storeId },
        });

        // ── Accessories ── (3 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Silver Chain Bracelet", Price = 150, Count = 20, Color = "Silver", Size = "One Size", Gender = "Men", Brand = "Local", CategoryId = cats[5].Id, StoreId = storeId },
            new Product { Title = "Leather Belt", Price = 120, Count = 25, Color = "Brown", Size = "M", Gender = "Men", Brand = "Local", CategoryId = cats[5].Id, StoreId = storeId },
            new Product { Title = "Aviator Sunglasses", Price = 200, Count = 15, Color = "Black", Size = "One Size", Gender = "Men", Brand = "Ray-Ban", CategoryId = cats[5].Id, StoreId = storeId },
        });

        // ── Outerwear ── (2 Women, 1 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Wool Overcoat", Price = 850, Count = 5, Color = "Charcoal", Size = "L", Gender = "Men", Brand = "Zara", CategoryId = cats[6].Id, StoreId = storeId },
            new Product { Title = "Leather Jacket", Price = 720, Count = 6, Color = "Black", Size = "M", Gender = "Women", Brand = "Mango", CategoryId = cats[6].Id, StoreId = storeId },
            new Product { Title = "Puffer Vest", Price = 340, Count = 9, Color = "Olive", Size = "M", Gender = "Women", Brand = "H&M", CategoryId = cats[6].Id, StoreId = storeId },
        });

        // ── Activewear ── (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Training Leggings", Price = 260, Count = 18, Color = "Black", Size = "S", Gender = "Women", Brand = "Nike", CategoryId = cats[7].Id, StoreId = storeId },
            new Product { Title = "Performance T-Shirt", Price = 190, Count = 22, Color = "Gray", Size = "L", Gender = "Men", Brand = "Adidas", CategoryId = cats[7].Id, StoreId = storeId },
            new Product { Title = "Running Shoes", Price = 450, Count = 14, Color = "Blue", Size = "42", Gender = "Men", Brand = "Nike", CategoryId = cats[7].Id, StoreId = storeId },
        });

        db.Products.AddRange(seedProducts);
        await db.SaveChangesAsync();
    }
}

// ── HTTP pipeline ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

// ── Global error handling ────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();
