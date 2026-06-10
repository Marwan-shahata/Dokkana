# Dokkana — دكانة

AI-Powered Local Fashion Discovery Platform · ITI .NET Full Stack Graduation Project.

Shoppers search local stores in natural language (Arabic / English); store owners
manage catalogs and ask an AI Business Copilot about their sales and stock.

> **This is the foundation / base only.** Folder structure, layers, and a few sample
> files are in place so the team can build features on top. Most folders are
> intentionally empty (with a `.gitkeep`) — that's where your sliced tasks go.

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
- **AI:** Semantic Kernel, OpenAI `gpt-4o-mini` + `text-embedding-3-small`
- **RAG:** Supabase pgvector, PdfPig, background `IHostedService` ingestion
- **Frontend:** Angular 20, Tailwind CSS (to add)

## Roles

`Admin` · `StoreOwner` · `Shopper` — seeded automatically on first run.

## Getting started

### Backend
```bash
cd backend/Dokkana.API
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."          # never commit the key
dotnet user-secrets set "Jwt:SecretKey" "<32+ char random>"
dotnet ef migrations add InitialCreate --project ../Dokkana.DAL --startup-project .
dotnet ef database update --project ../Dokkana.DAL --startup-project .
dotnet run
```

### Frontend
```bash
cd frontend
npm install
npm start            # http://localhost:4200
```

## What's already scaffolded

| Area | Status |
|---|---|
| 4-layer backend solution, wired references | ✅ builds clean |
| Models: Store, Product, Category, CatalogDocument, DocumentChunk, User/Role | ✅ |
| Generic Repository + UnitOfWork | ✅ |
| JWT + Identity + 3-role seeding | ✅ |
| Sample `StoresController` + `AiSearchController` (thin → manager/agent) | ✅ sample |
| AI subsystem folders (Agent stub, Plugins, Rag, Ingestion, Prompts) | 🟡 stubbed |
| Angular app + core/shared/features folder tree | ✅ scaffold |
| EF migration / database | ⬜ run the commands above |
| Auth token generation, products, cart, orders, real AI, Angular features | ⬜ team builds |

## Team conventions

- Branch per feature; PR into `main`. Don't push directly to `main`.
- Keep `Dokkana.BLL/DTOs` (backend) and `frontend/src/app/core/models` in sync.
- Never commit secrets — use `dotnet user-secrets` (backend) and `.env` (frontend).
