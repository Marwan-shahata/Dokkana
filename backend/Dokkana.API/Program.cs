using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Dokkana.API.Hubs;
using Dokkana.API.Services;
using Dokkana.BLL.AI.Agents;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.AI.WebSearch;
using Dokkana.BLL.AI.Copilot;
using Dokkana.BLL.AI.Catalog;
using Dokkana.BLL.AI.Embeddings;
using Dokkana.BLL.AI.Orchestrator;
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

// Avoid Windows Event Log writes in local/restricted desktop sessions. Console
// and debug logs are enough for development and do not require OS log access.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    var dataProtectionKeyPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data",
        "DataProtectionKeys");

    Directory.CreateDirectory(dataProtectionKeyPath);

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
        .SetApplicationName("Dokkana");
}

// Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SignalR (live notifications pushed to the bell icon)
builder.Services.AddSignalR();

// Rate limiting (protect the AI endpoint from abuse)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddFixedWindowLimiter("ai", opt =>
    {
        opt.PermitLimit = 15;            // 15 AI searches
        opt.Window = TimeSpan.FromMinutes(1); // per minute per server
        opt.QueueLimit = 0;
    });
});

// CORS (allow configured frontend origins; tighten methods/headers for production)
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? ["http://localhost:4200", "http://127.0.0.1:4200"];

    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(allowedOrigins)
              .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With", "X-SignalR-User-Agent")
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
              // Required for the SignalR notifications hub (credentialed websocket).
              .AllowCredentials());
});

// Database. Production/base config uses SQL Server. Development can opt into an
// in-memory store so the app still runs on machines where LocalDB/SQLEXPRESS
// Windows authentication is not available.
var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase(defaultConnection);
        return;
    }

    options.UseSqlServer(
        defaultConnection,
        // Retry transient connection failures - e.g. when (localdb) is still
        // spinning up from a cold/stopped state at startup.
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null));
});

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT authentication
// Resolve the signing secret once and keep it in configuration so the token
// issuer (AuthManager) and the validator below use the exact same key. The
// secret must never be the committed placeholder: fail fast in production, and
// fall back to an explicit dev-only key (with a warning) so local runs work.
const string JwtPlaceholder = "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS";
var jwtKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == JwtPlaceholder || jwtKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "dokkana-development-only-insecure-signing-key-change-me";
        builder.Configuration["Jwt:SecretKey"] = jwtKey;
        Console.WriteLine("[WARN] Jwt:SecretKey is not configured securely. Using an insecure development key. " +
                          "Set a real secret via user-secrets or environment before deploying.");
    }
    else
    {
        throw new InvalidOperationException(
            "Jwt:SecretKey must be set to a secure value (at least 32 characters) via configuration or secrets.");
    }
}
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

        // Browsers can't set Authorization headers on a websocket handshake, so
        // SignalR passes the JWT as ?access_token=... — read it for hub requests.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("StoreOwnerOnly", p => p.RequireRole("StoreOwner"));
    options.AddPolicy("ShopperOnly", p => p.RequireRole("Shopper"));
    // Store owners edit their own store/products; admins may edit any (the
    // managers enforce ownership and honour the admin override).
    options.AddPolicy("AdminOrStoreOwner", p => p.RequireRole("Admin", "StoreOwner"));
});

// Dependency Injection: DAL + BLL
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStoreManager, StoreManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IProductManager, ProductManager>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<ICartManager, CartManager>();
builder.Services.AddScoped<IOrderManager, OrderManager>();
builder.Services.AddScoped<IUserAdminManager, UserAdminManager>();
builder.Services.AddScoped<IWishlistManager, WishlistManager>();
builder.Services.AddScoped<IStoreFollowManager, StoreFollowManager>();
builder.Services.AddScoped<IReviewManager, ReviewManager>();
builder.Services.AddScoped<IAuditLogManager, AuditLogManager>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<INotificationManager, NotificationManager>();
builder.Services.AddScoped<INotificationRealtimeService, SignalRNotificationRealtimeService>();
builder.Services.AddScoped<IDashboardManager, DashboardManager>();
builder.Services.AddScoped<IMonetizationManager, MonetizationManager>();
builder.Services.AddHostedService<ReservationExpiryHostedService>();

// ── AI subsystem (OpenRouter + Serper) ────────────────────────────────────
// Keys are read from configuration / user-secrets. With no key the services
// still resolve; the agents return a graceful message instead of an answer.
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
builder.Services.Configure<SerperOptions>(builder.Configuration.GetSection("Serper"));

// OpenRouter chat + embeddings + Serper web search (typed HttpClients).
builder.Services.AddHttpClient<IOpenRouterClient, OpenRouterClient>();
builder.Services.AddHttpClient<IEmbeddingService, OpenRouterEmbeddingService>();
builder.Services.AddHttpClient<IWebSearchAgent, SerperWebSearchAgent>();

// Product-search agents read the catalog via the AI-specific product manager.
// Fully qualified because IProductManager also exists in Dokkana.BLL.Managers.
builder.Services.AddScoped<Dokkana.BLL.Managers.Products.IProductManager,
                           Dokkana.BLL.Managers.Products.ProductManager>();
builder.Services.AddScoped<IAiSearchManager, AiSearchManager>();
builder.Services.AddScoped<IInputInterpreterAgent, InputInterpreterAgent>();
builder.Services.AddScoped<ILocalSearchAgent, LocalSearchAgent>();
builder.Services.AddScoped<IRankingAgent, RankingAgent>();

// Product RAG: semantic (vector) search over the live catalog.
builder.Services.AddScoped<Dokkana.BLL.AI.Search.IProductSemanticSearchService,
                           Dokkana.BLL.AI.Search.ProductSemanticSearchService>();

// Background worker: keeps the product embeddings up to date automatically
// (new products become semantically searchable without a manual /reindex).
builder.Services.AddHostedService<Dokkana.BLL.AI.BackgroundServices.ProductEmbeddingBackgroundService>();

// Business Copilot (store analytics over real completed orders).
builder.Services.AddScoped<IBusinessCopilotManager, BusinessCopilotManager>();
builder.Services.AddScoped<IBusinessIntentAgent, BusinessIntentAgent>();
builder.Services.AddScoped<ISalesAnalyticsManager, SalesAnalyticsManager>();
builder.Services.AddScoped<IInventoryAnalyticsManager, InventoryAnalyticsManager>();

// Catalog RAG (PDF upload, chunking, embeddings, hybrid retrieval).
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextChunkingService, TextChunkingService>();
builder.Services.AddScoped<ICatalogRagManager, CatalogRagManager>();
builder.Services.AddScoped<ICatalogRetrievalService, CatalogRetrievalService>();
builder.Services.AddScoped<ICatalogEmbeddingMaintenanceService, CatalogEmbeddingMaintenanceService>();

// Unified orchestrator (POST /api/ai/ask) that routes to the tools above.
builder.Services.AddScoped<IRouteClassifierAgent, RouteClassifierAgent>();
builder.Services.AddScoped<IDokkanaAgentManager, DokkanaAgentManager>();

var app = builder.Build();

// Apply migrations + seed data
using (var scope = app.Services.CreateScope())
{
    // Ensure the database exists and all migrations (including Cart/Order tables)
    // are applied before any seeding queries run. Without this a fresh database
    // - or one missing a newer migration - fails at startup or at cart/order time.
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    // Seed roles (Admin / StoreOwner / Shopper)
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    foreach (var role in new[] { "Admin", "StoreOwner", "Shopper" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole(role));
    }

    // Seed default categories
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

    // Seed admin user
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

    // Seed sample store + store-owner user
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

        var sampleStore = new Dokkana.DAL.Models.Store
        {
            Name = "Dokkana Sample Store",
            Description = "A curated fashion boutique featuring trending styles.",
            Address = "Cairo, Egypt",
            IsApproved = true,
            OwnerId = storeOwner.Id
        };

        db.Stores.Add(sampleStore);
        await db.SaveChangesAsync();

        storeOwner.StoreId = sampleStore.Id;
        await userManager.UpdateAsync(storeOwner);
    }

    var seededStoreOwner = await userManager.FindByEmailAsync("store@dokkana.com");
    if (seededStoreOwner is not null)
    {
        var seededStore = await db.Stores.FirstOrDefaultAsync(s => s.OwnerId == seededStoreOwner.Id);
        if (seededStore is not null && seededStoreOwner.StoreId != seededStore.Id)
        {
            seededStoreOwner.StoreId = seededStore.Id;
            await userManager.UpdateAsync(seededStoreOwner);
        }
    }

    // Seed products (12 Men + 12 Women, 3 per category)
    if (!db.Products.Any())
    {
        var cats = await db.Categories.ToListAsync();
        var storeId = (await db.Stores.FirstAsync()).Id;

        var seedProducts = new List<Dokkana.DAL.Models.Product>();

        // Dresses (3 Women)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Floral Maxi Dress", Price = 450, Count = 15, Color = "Red", Size = "M", Gender = "Women", Brand = "Zara", CategoryId = cats[0].Id, StoreId = storeId },
            new Product { Title = "Little Black Dress", Price = 320, Count = 20, Color = "Black", Size = "S", Gender = "Women", Brand = "H&M", CategoryId = cats[0].Id, StoreId = storeId },
            new Product { Title = "Summer Sundress", Price = 250, Count = 12, Color = "Yellow", Size = "L", Gender = "Women", Brand = "Mango", CategoryId = cats[0].Id, StoreId = storeId },
        });

        // Tops (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "White Cotton Blouse", Price = 180, Count = 25, Color = "White", Size = "M", Gender = "Women", Brand = "Zara", CategoryId = cats[1].Id, StoreId = storeId },
            new Product { Title = "Striped Linen Shirt", Price = 220, Count = 18, Color = "Blue", Size = "L", Gender = "Men", Brand = "Massimo Dutti", CategoryId = cats[1].Id, StoreId = storeId },
            new Product { Title = "Casual Denim Jacket Top", Price = 290, Count = 10, Color = "Denim", Size = "L", Gender = "Men", Brand = "Levi's", CategoryId = cats[1].Id, StoreId = storeId },
        });

        // Pants (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Tailored Trousers", Price = 350, Count = 14, Color = "Navy", Size = "32", Gender = "Men", Brand = "Massimo Dutti", CategoryId = cats[2].Id, StoreId = storeId },
            new Product { Title = "High-Waisted Jeans", Price = 280, Count = 22, Color = "Blue", Size = "30", Gender = "Women", Brand = "Levi's", CategoryId = cats[2].Id, StoreId = storeId },
            new Product { Title = "Wide-Leg Linen Pants", Price = 310, Count = 8, Color = "Beige", Size = "M", Gender = "Women", Brand = "Mango", CategoryId = cats[2].Id, StoreId = storeId },
        });

        // Shoes (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Leather Loafers", Price = 520, Count = 10, Color = "Brown", Size = "42", Gender = "Men", Brand = "Clarks", CategoryId = cats[3].Id, StoreId = storeId },
            new Product { Title = "White Sneakers", Price = 380, Count = 30, Color = "White", Size = "42", Gender = "Men", Brand = "Nike", CategoryId = cats[3].Id, StoreId = storeId },
            new Product { Title = "Elegant Heels", Price = 490, Count = 7, Color = "Black", Size = "38", Gender = "Women", Brand = "Zara", CategoryId = cats[3].Id, StoreId = storeId },
        });

        // Bags (2 Women, 1 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Tote Bag", Price = 420, Count = 9, Color = "Tan", Size = "One Size", Gender = "Women", Brand = "Zara", CategoryId = cats[4].Id, StoreId = storeId },
            new Product { Title = "Crossbody Bag", Price = 360, Count = 11, Color = "Black", Size = "One Size", Gender = "Women", Brand = "Mango", CategoryId = cats[4].Id, StoreId = storeId },
            new Product { Title = "Backpack", Price = 290, Count = 13, Color = "Gray", Size = "One Size", Gender = "Men", Brand = "H&M", CategoryId = cats[4].Id, StoreId = storeId },
        });

        // Accessories (3 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Silver Chain Bracelet", Price = 150, Count = 20, Color = "Silver", Size = "One Size", Gender = "Men", Brand = "Local", CategoryId = cats[5].Id, StoreId = storeId },
            new Product { Title = "Leather Belt", Price = 120, Count = 25, Color = "Brown", Size = "M", Gender = "Men", Brand = "Local", CategoryId = cats[5].Id, StoreId = storeId },
            new Product { Title = "Aviator Sunglasses", Price = 200, Count = 15, Color = "Black", Size = "One Size", Gender = "Men", Brand = "Ray-Ban", CategoryId = cats[5].Id, StoreId = storeId },
        });

        // Outerwear (2 Women, 1 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Wool Overcoat", Price = 850, Count = 5, Color = "Charcoal", Size = "L", Gender = "Men", Brand = "Zara", CategoryId = cats[6].Id, StoreId = storeId },
            new Product { Title = "Leather Jacket", Price = 720, Count = 6, Color = "Black", Size = "M", Gender = "Women", Brand = "Mango", CategoryId = cats[6].Id, StoreId = storeId },
            new Product { Title = "Puffer Vest", Price = 340, Count = 9, Color = "Olive", Size = "M", Gender = "Women", Brand = "H&M", CategoryId = cats[6].Id, StoreId = storeId },
        });

        // Activewear (1 Women, 2 Men)
        seedProducts.AddRange(new[]
        {
            new Product { Title = "Training Leggings", Price = 260, Count = 18, Color = "Black", Size = "S", Gender = "Women", Brand = "Nike", CategoryId = cats[7].Id, StoreId = storeId },
            new Product { Title = "Performance T-Shirt", Price = 190, Count = 22, Color = "Gray", Size = "L", Gender = "Men", Brand = "Adidas", CategoryId = cats[7].Id, StoreId = storeId },
            new Product { Title = "Running Shoes", Price = 450, Count = 14, Color = "Blue", Size = "42", Gender = "Men", Brand = "Nike", CategoryId = cats[7].Id, StoreId = storeId },
        });

        db.Products.AddRange(seedProducts);
        await db.SaveChangesAsync();
    }

    // Keep the demo catalog useful even for databases that were created before
    // the image/search seed data existed.
    await EnsureSeedProductCatalogAsync(db);

    // Rich monetization demo data (Pro store, sales, campaigns, leads, trends)
    // so the new features show real numbers on a fresh database. Idempotent.
    await Dokkana.API.Seed.DemoSeeder.SeedAsync(db, userManager);
}

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

// Global error handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Skip HTTPS redirect in Development so the Angular dev server can call the API
// over http://localhost:5034 without a self-signed-certificate prompt.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");


app.Run();

static async Task EnsureSeedProductCatalogAsync(AppDbContext db)
{
    var store = await db.Stores.FirstOrDefaultAsync();
    if (store is null)
        return;

    var categories = await db.Categories.ToListAsync();

    int CategoryId(params string[] names)
    {
        foreach (var name in names)
        {
            var category = categories.FirstOrDefault(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (category is not null)
                return category.Id;
        }

        return categories.First().Id;
    }

    var seedProducts = new[]
    {
        new
        {
            Title = "Floral Maxi Dress",
            Description = "Red floral maxi dress for evening outings and summer events.",
            Price = 450m,
            Count = 15,
            Color = "Red",
            Size = "M",
            Gender = "Women",
            Brand = "Zara",
            CategoryId = CategoryId("Dresses"),
            ImageUrl = "/seed-products/floral-maxi-dress.svg"
        },
        new
        {
            Title = "Little Black Dress",
            Description = "Classic black short dress for parties, dinners, and smart casual looks.",
            Price = 320m,
            Count = 20,
            Color = "Black",
            Size = "S",
            Gender = "Women",
            Brand = "H&M",
            CategoryId = CategoryId("Dresses"),
            ImageUrl = "/seed-products/little-black-dress.svg"
        },
        new
        {
            Title = "Summer Sundress",
            Description = "Light yellow sundress for warm daytime plans.",
            Price = 250m,
            Count = 12,
            Color = "Yellow",
            Size = "L",
            Gender = "Women",
            Brand = "Mango",
            CategoryId = CategoryId("Dresses"),
            ImageUrl = "/seed-products/summer-sundress.svg"
        },
        new
        {
            Title = "Red Wedding Dress",
            Description = "Elegant red wedding dress with a formal gown silhouette for bridal events.",
            Price = 1250m,
            Count = 4,
            Color = "Red",
            Size = "M",
            Gender = "Women",
            Brand = "Layla Bridal",
            CategoryId = CategoryId("Dresses"),
            ImageUrl = "/seed-products/red-wedding-dress.svg"
        },
        new
        {
            Title = "White Cotton Blouse",
            Description = "Clean white cotton blouse for office and everyday outfits.",
            Price = 180m,
            Count = 25,
            Color = "White",
            Size = "M",
            Gender = "Women",
            Brand = "Zara",
            CategoryId = CategoryId("Tops", "Shirts"),
            ImageUrl = "/seed-products/white-cotton-blouse.svg"
        },
        new
        {
            Title = "Striped Linen Shirt",
            Description = "Blue striped linen shirt with a relaxed summer fit.",
            Price = 220m,
            Count = 18,
            Color = "Blue",
            Size = "L",
            Gender = "Men",
            Brand = "Massimo Dutti",
            CategoryId = CategoryId("Tops", "Shirts"),
            ImageUrl = "/seed-products/striped-linen-shirt.svg"
        },
        new
        {
            Title = "Black Polo Shirt",
            Description = "Black polo shirt for men, soft cotton collar tee, smart casual everyday wear.",
            Price = 260m,
            Count = 18,
            Color = "Black",
            Size = "L",
            Gender = "Men",
            Brand = "Dokkana Basics",
            CategoryId = CategoryId("Tops", "Shirts"),
            ImageUrl = "/seed-products/black-polo-shirt.svg"
        },
        new
        {
            Title = "Casual Denim Jacket Top",
            Description = "Denim blue jacket-style top for layered casual outfits.",
            Price = 290m,
            Count = 10,
            Color = "Denim",
            Size = "L",
            Gender = "Men",
            Brand = "Levi's",
            CategoryId = CategoryId("Tops", "Outerwear"),
            ImageUrl = "/seed-products/denim-jacket-top.svg"
        },
        new
        {
            Title = "Tailored Trousers",
            Description = "Navy tailored trousers for formal and smart casual fits.",
            Price = 350m,
            Count = 14,
            Color = "Navy",
            Size = "32",
            Gender = "Men",
            Brand = "Massimo Dutti",
            CategoryId = CategoryId("Pants"),
            ImageUrl = "/seed-products/tailored-trousers.svg"
        },
        new
        {
            Title = "High-Waisted Jeans",
            Description = "Blue high-waisted denim jeans with a straight leg.",
            Price = 280m,
            Count = 22,
            Color = "Blue",
            Size = "30",
            Gender = "Women",
            Brand = "Levi's",
            CategoryId = CategoryId("Pants"),
            ImageUrl = "/seed-products/high-waisted-jeans.svg"
        },
        new
        {
            Title = "Wide-Leg Linen Pants",
            Description = "Beige wide-leg linen pants for easy summer styling.",
            Price = 310m,
            Count = 8,
            Color = "Beige",
            Size = "M",
            Gender = "Women",
            Brand = "Mango",
            CategoryId = CategoryId("Pants"),
            ImageUrl = "/seed-products/wide-leg-linen-pants.svg"
        },
        new
        {
            Title = "Leather Loafers",
            Description = "Brown leather loafers for classic men's outfits.",
            Price = 520m,
            Count = 10,
            Color = "Brown",
            Size = "42",
            Gender = "Men",
            Brand = "Clarks",
            CategoryId = CategoryId("Shoes"),
            ImageUrl = "/seed-products/leather-loafers.svg"
        },
        new
        {
            Title = "White Sneakers",
            Description = "White sneakers for everyday streetwear and casual outfits.",
            Price = 380m,
            Count = 30,
            Color = "White",
            Size = "42",
            Gender = "Men",
            Brand = "Nike",
            CategoryId = CategoryId("Shoes"),
            ImageUrl = "/seed-products/white-sneakers.svg"
        },
        new
        {
            Title = "Elegant Heels",
            Description = "Black elegant heels for formal looks and evening events.",
            Price = 490m,
            Count = 7,
            Color = "Black",
            Size = "38",
            Gender = "Women",
            Brand = "Zara",
            CategoryId = CategoryId("Shoes"),
            ImageUrl = "/seed-products/elegant-heels.svg"
        },
        new
        {
            Title = "Tote Bag",
            Description = "Tan tote bag with room for everyday essentials.",
            Price = 420m,
            Count = 9,
            Color = "Tan",
            Size = "One Size",
            Gender = "Women",
            Brand = "Zara",
            CategoryId = CategoryId("Bags"),
            ImageUrl = "/seed-products/tote-bag.svg"
        },
        new
        {
            Title = "Crossbody Bag",
            Description = "Black crossbody bag for compact daily carry.",
            Price = 360m,
            Count = 11,
            Color = "Black",
            Size = "One Size",
            Gender = "Women",
            Brand = "Mango",
            CategoryId = CategoryId("Bags"),
            ImageUrl = "/seed-products/crossbody-bag.svg"
        },
        new
        {
            Title = "Backpack",
            Description = "Gray backpack for commuting, school, and short trips.",
            Price = 290m,
            Count = 13,
            Color = "Gray",
            Size = "One Size",
            Gender = "Men",
            Brand = "H&M",
            CategoryId = CategoryId("Bags"),
            ImageUrl = "/seed-products/backpack.svg"
        },
        new
        {
            Title = "Silver Chain Bracelet",
            Description = "Silver chain bracelet accessory with a clean polished finish.",
            Price = 150m,
            Count = 20,
            Color = "Silver",
            Size = "One Size",
            Gender = "Men",
            Brand = "Local",
            CategoryId = CategoryId("Accessories"),
            ImageUrl = "/seed-products/silver-chain-bracelet.svg"
        },
        new
        {
            Title = "Leather Belt",
            Description = "Brown leather belt with a simple everyday buckle.",
            Price = 120m,
            Count = 25,
            Color = "Brown",
            Size = "M",
            Gender = "Men",
            Brand = "Local",
            CategoryId = CategoryId("Accessories"),
            ImageUrl = "/seed-products/leather-belt.svg"
        },
        new
        {
            Title = "Aviator Sunglasses",
            Description = "Black aviator sunglasses with a timeless metal frame.",
            Price = 200m,
            Count = 15,
            Color = "Black",
            Size = "One Size",
            Gender = "Men",
            Brand = "Ray-Ban",
            CategoryId = CategoryId("Accessories"),
            ImageUrl = "/seed-products/aviator-sunglasses.svg"
        },
        new
        {
            Title = "Wool Overcoat",
            Description = "Charcoal wool overcoat for cold weather and formal outfits.",
            Price = 850m,
            Count = 5,
            Color = "Charcoal",
            Size = "L",
            Gender = "Men",
            Brand = "Zara",
            CategoryId = CategoryId("Outerwear"),
            ImageUrl = "/seed-products/wool-overcoat.svg"
        },
        new
        {
            Title = "Leather Jacket",
            Description = "Black leather jacket with a fitted biker-inspired cut.",
            Price = 720m,
            Count = 6,
            Color = "Black",
            Size = "M",
            Gender = "Women",
            Brand = "Mango",
            CategoryId = CategoryId("Outerwear"),
            ImageUrl = "/seed-products/leather-jacket.svg"
        },
        new
        {
            Title = "Puffer Vest",
            Description = "Olive puffer vest for lightweight layering.",
            Price = 340m,
            Count = 9,
            Color = "Olive",
            Size = "M",
            Gender = "Women",
            Brand = "H&M",
            CategoryId = CategoryId("Outerwear"),
            ImageUrl = "/seed-products/puffer-vest.svg"
        },
        new
        {
            Title = "Training Leggings",
            Description = "Black training leggings for gym, yoga, and activewear outfits.",
            Price = 260m,
            Count = 18,
            Color = "Black",
            Size = "S",
            Gender = "Women",
            Brand = "Nike",
            CategoryId = CategoryId("Activewear"),
            ImageUrl = "/seed-products/training-leggings.svg"
        },
        new
        {
            Title = "Performance T-Shirt",
            Description = "Gray performance t-shirt for workouts and running.",
            Price = 190m,
            Count = 22,
            Color = "Gray",
            Size = "L",
            Gender = "Men",
            Brand = "Adidas",
            CategoryId = CategoryId("Activewear", "Tops"),
            ImageUrl = "/seed-products/performance-t-shirt.svg"
        },
        new
        {
            Title = "Running Shoes",
            Description = "Blue running shoes with a breathable athletic upper.",
            Price = 450m,
            Count = 14,
            Color = "Blue",
            Size = "42",
            Gender = "Men",
            Brand = "Nike",
            CategoryId = CategoryId("Activewear", "Shoes"),
            ImageUrl = "/seed-products/running-shoes.svg"
        }
    };

    var changed = false;
    foreach (var seed in seedProducts)
    {
        var product = await db.Products.FirstOrDefaultAsync(p =>
            p.StoreId == store.Id && p.Title == seed.Title);

        if (product is null)
        {
            db.Products.Add(new Product
            {
                Title = seed.Title,
                Description = seed.Description,
                Price = seed.Price,
                Count = seed.Count,
                Color = seed.Color,
                Size = seed.Size,
                Gender = seed.Gender,
                Brand = seed.Brand,
                CategoryId = seed.CategoryId,
                StoreId = store.Id,
                ImageUrl = seed.ImageUrl
            });
            changed = true;
            continue;
        }

        if (string.IsNullOrWhiteSpace(product.Description))
        {
            product.Description = seed.Description;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            product.ImageUrl = seed.ImageUrl;
            changed = true;
        }
    }

    if (changed)
        await db.SaveChangesAsync();
}
