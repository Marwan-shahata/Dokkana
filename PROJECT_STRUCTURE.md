# Dokkana — Project Architecture & Version History

> **Project:** AI-Powered Local Fashion Discovery Platform  
> **Stack:** .NET 10 (ASP.NET Core Web API) + Angular 20 SPA  
> **Repository:** [github.com/mazengehad/iti_final_project](https://github.com/mazengehad/iti_final_project)

---

## Project Purpose

**Dokkana (دكانة)** is an AI-powered local fashion discovery platform built as the ITI .NET Full Stack Graduation Project.

- **Shoppers** can search local stores using natural language in **Arabic or English** to find fashion products nearby.
- **Store owners** can manage their catalogs and ask an **AI Business Copilot** about their sales and stock.
- **Admins** oversee stores, approve new listings, and manage the platform.

> **Current status:** Foundation/base version with backend MVP complete (Auth, Stores, Products, Categories). Frontend has a styled landing page with all feature folders scaffolded as placeholders.

---

## Architecture Overview

The project follows a **Clean N-Tier Architecture** with 4 backend layers and a separate Angular SPA frontend:

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (Angular 20)                  │
│  ┌─────────┐ ┌──────────┐ ┌────────────┐ ┌──────────┐  │
│  │   Core   │ │ Features │ │  Shared    │ │   App    │  │
│  │(services,│ │ (auth,   │ │(components,│ │ (root    │  │
│  │ guards,  │ │ products,│ │  pipes)    │ │  layout) │  │
│  │ models)  │ │ stores…) │ │            │ │          │  │
│  └────┬─────┘ └────┬─────┘ └─────┬──────┘ └──────────┘  │
│       └────────────┴─────────────┘                      │
│                    │  HTTP (JWT)                         │
└────────────────────┼────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────┐
│              Backend (ASP.NET Core Web API)               │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Dokkana.API  (Presentation Layer)                │   │
│  │  • Controllers (Auth, Stores, Products,           │   │
│  │     Categories, AiSearch)                         │   │
│  │  • ExceptionHandlingMiddleware                    │   │
│  │  • JWT Auth · CORS · Middleware · OpenAPI/Scalar  │   │
│  └──────────────────────┬───────────────────────────┘   │
│                         │                                │
│  ┌──────────────────────▼───────────────────────────┐   │
│  │  Dokkana.BLL  (Business Logic Layer)              │   │
│  │  • Managers (Auth, Store, Product, Category)      │   │
│  │  • Services (FileService)                         │   │
│  │  • AI Subsystem (Agent, Plugins, RAG, Ingestion)  │   │
│  │  • DTOs (Auth, Store, Product, Category, AI)      │   │
│  └──────────────────────┬───────────────────────────┘   │
│                         │                                │
│  ┌──────────────────────▼───────────────────────────┐   │
│  │  Dokkana.DAL  (Data Access Layer)                 │   │
│  │  • EF Core DbContext · SQL Server                 │   │
│  │  • Models (Store, Product, Category, User…)       │   │
│  │  • Generic Repository + UnitOfWork                │   │
│  │  • EF Migrations                                  │   │
│  └──────────────────────┬───────────────────────────┘   │
│                         │                                │
│  ┌──────────────────────▼───────────────────────────┐   │
│  │  Dokkana.Common  (Shared Primitives)              │   │
│  │  • GeneralResult<T> · PagedResult · Errors        │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### Backend Layer Dependencies

| Layer | Project | Dependencies |
|-------|---------|-------------|
| **Presentation** | `Dokkana.API` | BLL, Common |
| **Business Logic** | `Dokkana.BLL` | DAL, Common |
| **Data Access** | `Dokkana.DAL` | Common |
| **Shared** | `Dokkana.Common` | None |

### Frontend Module Architecture

```
frontend/src/app/
├── app.ts / app.html / app.css     → Root component (landing page)
├── app.config.ts                    → Angular app configuration
├── app.routes.ts                    → Route definitions (empty shell)
├── core/                            → Shared infrastructure (all placeholders)
│   ├── guards/                      → Route guards (AuthGuard, RoleGuard…)
│   ├── interceptors/                → HTTP interceptors (JWT, error…)
│   ├── models/                      → TypeScript interfaces (User, Product…)
│   └── services/                    → Shared services (ApiService, AuthService…)
├── features/                        → Feature modules (all placeholders)
│   ├── admin/                       → Admin dashboard
│   ├── ai-search/                   → AI-powered search
│   ├── auth/                        → Login / Register
│   ├── cart/                        → Shopping cart
│   ├── orders/                      → Order history
│   ├── products/                    → Product browsing
│   ├── store-portal/                → Store owner portal
│   └── stores/                      → Store discovery
└── shared/                          → Reusable pieces (all placeholders)
    ├── components/                  → Shared UI components
    └── pipes/                       → Custom pipes
```

### AI Subsystem (In Progress)

```
Dokkana.BLL/AI/
├── Agent/                           → Core AI agent
│   ├── IDokkanaAgent.cs             → Interface (AskAsync)
│   └── DokkanaAgent.cs              → Stub (not yet wired)
├── Prompts/                         → System prompt templates
│   └── AgentPrompts.cs              → ShoppingAssistant prompt
├── Plugins/                         → Semantic Kernel plugins (planned)
│   └── README.md                    → ProductSearch, CatalogRAG, BusinessCopilot
├── Rag/                             → RAG pipeline (planned)
│   └── README.md                    → PDF extraction, chunking, embeddings
├── Ingestion/                        → Background processing (planned)
│   └── README.md                    → Channel<T> queue + IHostedService worker
└── AGENTIC_AI_PLAN.md               → 5-phase implementation roadmap
```

---

## Data Model (ER)

```
ApplicationUser (IdentityUser)
  ├── FirstName, LastName
  ├── StoreId? ─────────────────────┐
  └── Store (nav)                   │
                                    ▼
Store                              Product
├── Id ◄───────────────────────────┼── StoreId (FK, Cascade)
├── Name, Description, Address     ├── CategoryId ──────┐
├── Latitude, Longitude, LogoUrl   ├── Title, Price     │
├── IsApproved                      │   Count, ImageUrl  │
└── OwnerId (FK → ApplicationUser)  │   Color, Size      │
                                    │   Brand, Gender    │
CatalogDocument                     │                    │
├── Id                              │                    │
├── StoreId (FK, Cascade)           │                    │
├── FileName, UploadedAt            │                    │
├── Status (Processing/Ready/Failed)│                    │
└── Chunks (nav → DocumentChunk)    ▼                    ▼
                                  Category              DocumentChunk
                                  ├── Id                ├── Id
                                  ├── Name, Description ├── StoreId
                                  ├── ImageUrl          ├── CatalogDocumentId
                                                        ├── Content, PageNumber
                                                        └── Embedding (float[1536] → pgvector)
```

---

## API Endpoints

### Auth — `api/auth`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/register` | Public | Register new user. If `StoreName` provided → StoreOwner + Store; else → Shopper. Returns JWT. |
| POST | `/api/auth/login` | Public | Login with email/password. Returns JWT with claims (NameIdentifier, Name, Email, Role). |
| POST | `/api/auth/logout` | Public | Stateless logout (client discards token). |

### Stores — `api/stores`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/stores` | Admin | Get all stores (including unapproved). |
| GET | `/api/stores/approved` | Public | Get only approved stores. |
| GET | `/api/stores/my` | StoreOwner | Get authenticated owner's store. |
| GET | `/api/stores/{id}` | Public | Get full store details (owner name, product count). |
| POST | `/api/stores` | StoreOwner | Create a new store (pending admin approval). |
| PUT | `/api/stores/{id}` | StoreOwner | Update store fields. |
| DELETE | `/api/stores/{id}` | Admin | Delete a store. |
| PUT | `/api/stores/{id}/approve` | Admin | Approve a store. |

### Products — `api/products`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/products` | Public | Search/filter products with pagination. Query params: `search`, `categoryId`, `minPrice`, `maxPrice`, `color`, `gender`, `page` (1), `pageSize` (10). |
| GET | `/api/products/{id}` | Public | Get full product details. |
| GET | `/api/products/store/{storeId}` | Public | Get paginated products for a store. Query: `page`, `pageSize`. |
| POST | `/api/products` | StoreOwner | Create a product. |
| PUT | `/api/products/{id}` | StoreOwner | Update a product (patch-style). |
| DELETE | `/api/products/{id}` | Auth | Delete a product. |

### Categories — `api/categories`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/categories` | Public | Get all categories (8 seeded: Dresses, Tops, Pants, Shoes, Bags, Accessories, Outerwear, Activewear). |
| GET | `/api/categories/{id}` | Public | Get category by ID. |
| POST | `/api/categories` | Admin | Create a new category. |
| PUT | `/api/categories/{id}` | Admin | Update a category. |
| DELETE | `/api/categories/{id}` | Admin | Delete a category. |

### AI Search — `api/ai`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/ai/search` | Public | Natural-language AI shopping search (currently a stub). |

### Response Format

All endpoints return wrapped responses:

```json
{
  "success": true,
  "message": null,
  "data": { ... }
}
```

Error responses:

```json
{
  "success": false,
  "message": "Not found.",
  "data": null
}
```

Paginated responses include `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

---

## Request DTOs

### Register
```json
{
  "firstName": "string (required, max 50)",
  "lastName": "string (required, max 50)",
  "email": "string (required, email, max 100)",
  "password": "string (required, min 4, max 100)",
  "phoneNumber": "string? (phone, max 20)",
  "storeName": "string? (max 100, if provided → creates StoreOwner role + store)"
}
```

### Login
```json
{
  "email": "string (required, email)",
  "password": "string (required)"
}
```

### Auth Response
```json
{
  "userId": "string",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "role": "string (Admin | StoreOwner | Shopper)",
  "token": "string (JWT, 7-day expiry)",
  "storeId": "int?",
  "storeName": "string?"
}
```

### Store Create
```json
{
  "name": "string (required, max 100)",
  "description": "string? (max 500)",
  "address": "string? (max 200)",
  "latitude": "double?",
  "longitude": "double?"
}
```

### Store Update
```json
{
  "name": "string? (max 100)",
  "description": "string? (max 500)",
  "logoUrl": "string? (max 300)",
  "address": "string? (max 200)",
  "latitude": "double?",
  "longitude": "double?"
}
```

### Product Create
```json
{
  "title": "string (required, max 200)",
  "description": "string? (max 1000)",
  "price": "decimal (required, > 0)",
  "count": "int (required, >= 0)",
  "imageUrl": "string? (max 300)",
  "color": "string? (max 50)",
  "size": "string? (max 20)",
  "brand": "string? (max 100)",
  "gender": "string? (max 20)",
  "categoryId": "int (required)",
  "storeId": "int (required)"
}
```

### Product Update
```json
{
  "title": "string? (max 200)",
  "description": "string? (max 1000)",
  "price": "decimal? (> 0)",
  "count": "int? (>= 0)",
  "imageUrl": "string? (max 300)",
  "color": "string? (max 50)",
  "size": "string? (max 20)",
  "brand": "string? (max 100)",
  "gender": "string? (max 20)",
  "categoryId": "int?"
}
```

### Category Create
```json
{
  "name": "string (required, max 100)",
  "description": "string? (max 500)",
  "imageUrl": "string? (max 300)"
}
```

### AI Search Request
```json
{
  "query": "string (required, natural language, e.g. 'red linen shirt under 300 EGP')",
  "storeId": "int? (optional, scope search to a specific store)"
}
```

### AI Search Response
```json
{
  "query": "string (echoed original query)",
  "answer": "string (AI-generated response)"
}
```

---

## Version History

### v0.4.0 — "Backend MVP: Products, Categories, Stores, File Upload" (2026-06-12)
**Complete marketplace backend — Sprint 1 & Sprint 2**

**New Files:**
- **Middleware:** `ExceptionHandlingMiddleware.cs` — global error handler, returns consistent `GeneralResult` JSON with proper HTTP status codes
- **Controllers:** `CategoriesController.cs` (GET public, POST/PUT/DELETE Admin), `ProductsController.cs` (GET paginated+filtered, POST/PUT/DELETE role-based)
- **Managers:** `ICategoryManager`/`CategoryManager` (full CRUD), `IProductManager`/`ProductManager` (full CRUD + paginated search with 6 filters)
- **Services:** `IFileService`/`FileService` — image upload to `wwwroot/uploads/`, validates file types, generates unique filenames
- **DTOs:** `CategoryDTO`, `CategoryCreateDTO`, `ProductCreateDTO`, `ProductUpdateDTO`, `ProductListDTO`, `ProductDetailsDTO`, `StoreUpdateDTO`, `StoreDetailsDTO`
- **Static:** `wwwroot/uploads/` directory for uploaded images

**Updated Files:**
- **`GenericRepository`** — added `FindAsync(predicate)`, `CountAsync()`, `Query()` for complex filtering
- **`IStoreManager`/`StoreManager`** — added `UpdateAsync`, `DeleteAsync`, `GetApprovedAsync`, `GetByOwnerAsync`, `GetDetailsAsync`
- **`StoresController`** — added GET `/approved`, GET `/my`, PUT update, DELETE. GET all now AdminOnly
- **All DTOs** — added Data Annotation validation
- **`Program.cs`** — registered all new managers/services in DI, added seed for 8 categories + admin user

---

### v0.3.1 — `89a6e18` Merge `AppDesignV01` into `main` (2026-06-10)
- Merge commit — no new file changes; integrates v0.3.0 style work into main branch.

---

### v0.3.0 — `646bb0f` "app style V01" (2026-06-10)
**Frontend visual design — complete marketing landing page**

- Full design system with CSS custom properties
- Rich landing page template with animated hero, features grid, product tabs, testimonials, cart sidebar
- Component logic with typed interfaces, tab filtering, pagination, cart management

---

### v0.2.0 — `2727282` "Add auth module: Register, Login, Logout with JWT" (2026-06-10)
**Authentication system — backend + frontend wiring**

- `AuthController` — register/login/logout with JWT
- `IAuthManager`/`AuthManager` — register with auto StoreOwner role, JWT generation (7-day expiry)
- Auth DTOs — `RegisterDTO`, `LoginDTO`, `AuthResponseDTO`
- EF Core `InitialCreate` migration
- `Program.cs` — Identity + JWT + role policies + seeding

---

### v0.1.0 — `39bda6a` "new update" (2026-06-10)
**Initial project scaffold — foundation & structure**

- Full 4-layer solution
- DAL: EF Core `AppDbContext`, all domain models, `GenericRepository<T>`, `IUnitOfWork`/`UnitOfWork`
- BLL: `StoreManager`, AI stubs
- API: `StoresController`, `AiSearchController`, `Program.cs`
- Common: `GeneralResult`, `PagedResult`, `Errors`

---

## Project File Tree

```
iti_final_project/
├── .github/
│   └── workflows/                    # CI/CD pipelines (empty)
│
├── backend/
│   ├── Dokkana.slnx                  # Solution file (.NET 10)
│   ├── Dokkana.API/                  # Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs     # POST register, login, logout
│   │   │   ├── StoresController.cs   # CRUD + approve (8 endpoints)
│   │   │   ├── ProductsController.cs # CRUD + search/filter (6 endpoints)
│   │   │   ├── CategoriesController.cs # CRUD (5 endpoints)
│   │   │   └── AiSearchController.cs # POST natural-language search
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs
│   │   ├── Program.cs                # Entry point, DI, middleware, seeding
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Properties/launchSettings.json
│   │   └── Dokkana.API.csproj
│   │
│   ├── Dokkana.BLL/                  # Business Logic Layer
│   │   ├── AI/
│   │   │   ├── Agent/
│   │   │   │   ├── IDokkanaAgent.cs
│   │   │   │   └── DokkanaAgent.cs   # Stub
│   │   │   ├── Prompts/
│   │   │   │   └── AgentPrompts.cs
│   │   │   ├── Plugins/README.md
│   │   │   ├── Rag/README.md
│   │   │   ├── Ingestion/README.md
│   │   │   └── AGENTIC_AI_PLAN.md
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterDTO.cs
│   │   │   │   ├── LoginDTO.cs
│   │   │   │   └── AuthResponseDTO.cs
│   │   │   ├── Store/
│   │   │   │   ├── StoreCreateDTO.cs
│   │   │   │   ├── StoreUpdateDTO.cs
│   │   │   │   ├── StoreDTO.cs
│   │   │   │   └── StoreDetailsDTO.cs
│   │   │   ├── Product/
│   │   │   │   ├── ProductCreateDTO.cs
│   │   │   │   ├── ProductUpdateDTO.cs
│   │   │   │   ├── ProductListDTO.cs
│   │   │   │   └── ProductDetailsDTO.cs
│   │   │   ├── Category/
│   │   │   │   ├── CategoryCreateDTO.cs
│   │   │   │   └── CategoryDTO.cs
│   │   │   └── AI/
│   │   │       ├── AiSearchRequestDTO.cs
│   │   │       └── AiSearchResponseDTO.cs
│   │   ├── Managers/
│   │   │   ├── IAuthManager.cs / AuthManager.cs
│   │   │   ├── IStoreManager.cs / StoreManager.cs
│   │   │   ├── IProductManager.cs / ProductManager.cs
│   │   │   └── ICategoryManager.cs / CategoryManager.cs
│   │   ├── Services/
│   │   │   ├── IFileService.cs
│   │   │   └── FileService.cs
│   │   └── Dokkana.BLL.csproj
│   │
│   ├── Dokkana.DAL/                  # Data Access Layer
│   │   ├── Data/AppDbContext.cs
│   │   ├── Models/
│   │   │   ├── ApplicationUser.cs
│   │   │   ├── ApplicationRole.cs
│   │   │   ├── Store.cs
│   │   │   ├── Category.cs
│   │   │   ├── Product.cs
│   │   │   ├── CatalogDocument.cs
│   │   │   └── DocumentChunk.cs
│   │   ├── Repositories/
│   │   │   ├── IGenericRepository.cs
│   │   │   └── GenericRepository.cs
│   │   ├── UnitOfWork/
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── UnitOfWork.cs
│   │   ├── Migrations/
│   │   │   ├── 20260610121114_InitialCreate.cs
│   │   │   ├── 20260610121114_InitialCreate.Designer.cs
│   │   │   └── AppDbContextModelSnapshot.cs
│   │   └── Dokkana.DAL.csproj
│   │
│   └── Dokkana.Common/              # Shared Primitives
│       ├── GeneralResult.cs
│       ├── PagedResult.cs
│       ├── Errors.cs
│       └── Dokkana.Common.csproj
│
├── frontend/
│   ├── src/
│   │   ├── index.html
│   │   ├── main.ts
│   │   ├── styles.css
│   │   ├── environments/
│   │   │   ├── environment.ts        # Dev (localhost:7000)
│   │   │   └── environment.prod.ts   # Production (/api)
│   │   └── app/
│   │       ├── app.ts / .html / .css
│   │       ├── app.config.ts
│   │       ├── app.routes.ts
│   │       ├── app.spec.ts
│   │       ├── core/
│   │       │   ├── guards/.gitkeep
│   │       │   ├── interceptors/.gitkeep
│   │       │   ├── models/.gitkeep
│   │       │   └── services/.gitkeep
│   │       ├── features/
│   │       │   ├── admin/.gitkeep
│   │       │   ├── ai-search/.gitkeep
│   │       │   ├── auth/.gitkeep
│   │       │   ├── cart/.gitkeep
│   │       │   ├── orders/.gitkeep
│   │       │   ├── products/.gitkeep
│   │       │   ├── store-portal/.gitkeep
│   │       │   └── stores/.gitkeep
│   │       └── shared/
│   │           ├── components/.gitkeep
│   │           └── pipes/.gitkeep
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json / tsconfig.app.json / tsconfig.spec.json
│   └── .editorconfig
│
├── .gitattributes
├── .gitignore
├── README.md
└── PROJECT_STRUCTURE.md
```

## Tech Stack

| Tier | Technology | Version |
|------|-----------|---------|
| **Runtime** | .NET | 10 |
| **Backend Framework** | ASP.NET Core Web API | 10 |
| **ORM** | Entity Framework Core | 10.0.8 |
| **Database** | SQL Server (LocalDB) | — |
| **Vector Store** | Supabase pgvector | (planned) |
| **Auth** | ASP.NET Core Identity + JWT Bearer | — |
| **AI Framework** | Semantic Kernel + OpenAI | (planned) |
| **AI Models** | gpt-4o-mini + text-embedding-3-small | — |
| **PDF** | PdfPig | (planned) |
| **Frontend** | Angular | 20 |
| **UI** | Tailwind CSS | (to add) |
| **API Docs** | Scalar / OpenAPI | — |
