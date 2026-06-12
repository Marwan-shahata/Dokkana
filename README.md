# Dokkana — دكانة

AI-Powered Local Fashion Discovery Platform · ITI .NET Full Stack Graduation Project.

Shoppers search local stores in natural language (Arabic / English); store owners
manage catalogs and ask an AI Business Copilot about their sales and stock.

> **This is the foundation / base only.** Backend MVP is complete (Auth, Stores,
> Products, Categories). Frontend has a styled landing page with all feature
> folders scaffolded as placeholders.

---

## Repository layout

```
Dokkana/
├── backend/          ASP.NET Core (.NET 10) — N-Tier Web API
│   ├── Dokkana.API/        Presentation: controllers, auth, DI, pipeline
│   ├── Dokkana.BLL/        Business logic: managers, DTOs, validators, AI/
│   ├── Dokkana.DAL/        Data access: EF Core, models, repositories, UoW
│   ├── Dokkana.Common/     Shared result wrappers (GeneralResult, PagedResult)
│   └── Dokkana.slnx        Solution file
│
└── frontend/         Angular 20 SPA (shopper UI · store portal · admin)
    └── src/app/{core, shared, features}/
```

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core, SQL Server, Identity + JWT
- **AI:** Semantic Kernel, OpenAI `gpt-4o-mini` + `text-embedding-3-small` (planned)
- **RAG:** Supabase pgvector, PdfPig, background `IHostedService` ingestion (planned)
- **Frontend:** Angular 20, Tailwind CSS (to add)

## Roles

`Admin` · `StoreOwner` · `Shopper` — seeded automatically on first run.

Default admin: `admin@dokkana.com` / `Admin123!`
Sample store owner: `store@dokkana.com` / `Store123!`

## Getting started

### Backend
```bash
cd backend/Dokkana.API
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "Jwt:SecretKey" "<32+ char random>"
dotnet ef migrations add InitialCreate --project ../Dokkana.DAL --startup-project .
dotnet ef database update --project ../Dokkana.DAL --startup-project .
dotnet run    # https://localhost:7000
```

### Frontend
```bash
cd frontend
npm install
npm start    # http://localhost:4200
```

### API Docs (Scalar)
Navigate to [https://localhost:7000/scalar/v1](https://localhost:7000/scalar/v1) when the backend is running.

---

## API Endpoints

All endpoints return wrapped responses in `GeneralResult<T>` format:

```json
// Success
{ "success": true, "message": null, "data": { ... } }

// Error
{ "success": false, "message": "Error description.", "data": null }

// Paginated
{ "success": true, "message": null, "data": {
    "items": [...], "page": 1, "pageSize": 10,
    "totalCount": 42, "totalPages": 5
} }
```

### Authentication — `POST /api/auth`

Register a new user:

```bash
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Ahmed",
    "lastName": "Ali",
    "email": "ahmed@example.com",
    "password": "pass1234",
    "storeName": "Ahmed Fashion"      # omit to create a Shopper account
  }'
```

Login:

```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@dokkana.com", "password": "Admin123!" }'
```

Response includes a JWT `token`. Use it in subsequent requests:

```bash
# Store the token
TOKEN="eyJhbGciOiJI...<your-token>"

# Use in Authorization header
curl -H "Authorization: Bearer $TOKEN" https://localhost:7000/api/stores/my
```

Logout:

```bash
curl -X POST https://localhost:7000/api/auth/logout
```

### Stores — `/api/stores`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/stores` | Admin | All stores (incl. unapproved) |
| GET | `/api/stores/approved` | Public | Approved stores only |
| GET | `/api/stores/my` | StoreOwner | Your store |
| GET | `/api/stores/{id}` | Public | Store details |
| POST | `/api/stores` | StoreOwner | Create store |
| PUT | `/api/stores/{id}` | StoreOwner | Update store |
| DELETE | `/api/stores/{id}` | Admin | Delete store |
| PUT | `/api/stores/{id}/approve` | Admin | Approve store |

```bash
# Get approved stores (public)
curl https://localhost:7000/api/stores/approved

# Create a store (StoreOwner)
curl -X POST https://localhost:7000/api/stores \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "name": "My Boutique", "description": "Modern fashion", "address": "Cairo" }'

# Approve a store (Admin)
curl -X PUT https://localhost:7000/api/stores/1/approve \
  -H "Authorization: Bearer $TOKEN"
```

### Products — `/api/products`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/products` | Public | Search + filter (query params: `search`, `categoryId`, `minPrice`, `maxPrice`, `color`, `gender`, `page`, `pageSize`) |
| GET | `/api/products/{id}` | Public | Product details |
| GET | `/api/products/store/{storeId}` | Public | Products by store (`page`, `pageSize`) |
| POST | `/api/products` | StoreOwner | Create product |
| PUT | `/api/products/{id}` | StoreOwner | Update product |
| DELETE | `/api/products/{id}` | Auth | Delete product |

```bash
# Search products (public)
curl "https://localhost:7000/api/products?search=dress&minPrice=100&maxPrice=500&page=1&pageSize=20"

# Create a product (StoreOwner)
curl -X POST https://localhost:7000/api/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Red Linen Dress",
    "price": 299.99,
    "count": 10,
    "color": "Red",
    "size": "M",
    "gender": "Women",
    "categoryId": 1,
    "storeId": 1
  }'
```

### Categories — `/api/categories`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/categories` | Public | All categories (8 seeded) |
| GET | `/api/categories/{id}` | Public | Category by ID |
| POST | `/api/categories` | Admin | Create category |
| PUT | `/api/categories/{id}` | Admin | Update category |
| DELETE | `/api/categories/{id}` | Admin | Delete category |

```bash
# List all categories (public)
curl https://localhost:7000/api/categories
```

### AI Search — `/api/ai`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/ai/search` | Public | Natural-language search (currently a stub) |

```bash
curl -X POST https://localhost:7000/api/ai/search \
  -H "Content-Type: application/json" \
  -d '{ "query": "red linen shirt under 300 EGP" }'
```

---

## Seed Data

On first run, the application seeds:
- **3 roles:** Admin, StoreOwner, Shopper
- **8 categories:** Dresses, Tops, Pants, Shoes, Bags, Accessories, Outerwear, Activewear
- **1 admin user:** `admin@dokkana.com` / `Admin123!`
- **1 store owner user:** `store@dokkana.com` / `Store123!`
- **1 approved store:** Dokkana Sample Store
- **24 products** (3 per category):

| Category | Products |
|---|---|
| Dresses | Floral Maxi Dress (450 EGP), Little Black Dress (320 EGP), Summer Sundress (250 EGP) |
| Tops | White Cotton Blouse (180 EGP), Striped Linen Shirt (220 EGP), Casual Denim Jacket Top (290 EGP) |
| Pants | Tailored Trousers (350 EGP), High-Waisted Jeans (280 EGP), Wide-Leg Linen Pants (310 EGP) |
| Shoes | Leather Loafers (520 EGP), White Sneakers (380 EGP), Elegant Heels (490 EGP) |
| Bags | Tote Bag (420 EGP), Crossbody Bag (360 EGP), Backpack (290 EGP) |
| Accessories | Gold Chain Necklace (150 EGP), Leather Belt (120 EGP), Aviator Sunglasses (200 EGP) |
| Outerwear | Wool Overcoat (850 EGP), Leather Jacket (720 EGP), Puffer Vest (340 EGP) |
| Activewear | Training Leggings (260 EGP), Performance T-Shirt (190 EGP), Running Shoes (450 EGP) |

## What's already scaffolded

| Area | Status |
|---|---|
| 4-layer backend solution, wired references | ✅ builds clean |
| Models: Store, Product, Category, CatalogDocument, DocumentChunk, User/Role | ✅ |
| Generic Repository + UnitOfWork | ✅ |
| JWT + Identity + 3-role seeding | ✅ |
| Auth: register/login/logout with JWT | ✅ |
| Stores: CRUD + approve (8 endpoints) | ✅ |
| Products: CRUD + search/filter (6 endpoints) | ✅ |
| Categories: CRUD (5 endpoints) | ✅ |
| Global exception handling middleware | ✅ |
| File upload service | ✅ wired |
| AI subsystem folders (Agent stub, Plugins, Rag, Ingestion, Prompts) | 🟡 stubbed |
| Angular app + core/shared/features folder tree | ✅ scaffold |
| Landing page with full design system | ✅ built |
| EF migration / database | ✅ applied |
| Seed data (store, 24 products, categories, roles, users) | ✅ populated |
| Angular feature modules (auth, products, cart, etc.) | ⬜ team builds |

## Team conventions

- Branch per feature; PR into `main`. Don't push directly to `main`.
- Keep `Dokkana.BLL/DTOs` (backend) and `frontend/src/app/core/models` in sync.
- Never commit secrets — use `dotnet user-secrets` (backend) and `.env` (frontend).
