using System.Text;
using Dokkana.BLL.AI.Agent;
using Dokkana.BLL.AI.Plugins;
using Dokkana.BLL.Managers;
using Dokkana.BLL.Managers.Products;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
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
builder.Services.AddScoped<IProductManager, ProductManager>();

// ── AI: OpenAI + Semantic Kernel ─────────────────────────────────────────────
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
var chatModel = builder.Configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";

if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    throw new InvalidOperationException("OpenAI:ApiKey is missing. Add it using dotnet user-secrets.");
}

// Registers OpenAI chat completion service in Dependency Injection.
builder.Services.AddOpenAIChatCompletion(
    modelId: chatModel,
    apiKey: openAiApiKey);

// Register AI plugins.
builder.Services.AddScoped<ProductSearchPlugin>();

// Register Semantic Kernel and attach plugins to it.
builder.Services.AddScoped<Kernel>(serviceProvider =>
{
    var kernel = new Kernel(serviceProvider);

    var productSearchPlugin = serviceProvider.GetRequiredService<ProductSearchPlugin>();

    kernel.Plugins.AddFromObject(
        productSearchPlugin,
        pluginName: "ProductSearch");

    return kernel;
});

// Register main Dokkana AI agent.
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
        {
            await roleManager.CreateAsync(new ApplicationRole(role));
        }
    }

    // ── Seed demo fashion data for AI search testing ───────────────────────────
    var dbSeeder = new DbSeeder(
        scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>());

    await dbSeeder.SeedAsync();
}

// ── HTTP pipeline ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

if (!app.Environment.IsDevelopment() || app.Urls.Any(u => u.StartsWith("https")))
    app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();