# Dokkana — دكانة

AI-Powered Local Fashion Discovery Platform · ITI .NET Full Stack Graduation Project.

Shoppers search local stores in natural language (Arabic / English / Franco-Arabic);
store owners upload PDF catalogs, ask an AI **Business Copilot** about their sales and
stock, and shoppers get **semantic** product results with price, rating, and store
location.

## AI features (all implemented)

| Feature | Endpoint | What it does |
|---|---|---|
| **NL Product Search** | `POST /api/ai/search` | Hybrid **keyword + semantic (RAG)** search over the live catalog. Understands Arabic / English / Franco. Returns price, rating, store location + distance. Optional web results (Serper). |
| **Visual / Image Search** | `POST /api/ai/search/image` | Upload a photo → finds the most visually-similar products. The multimodal embedding matches the image against product vectors (no separate image DB). |
| **Business Copilot** | `POST /api/ai/copilot/ask` | Store-owner Q&A over **real completed orders** — best sellers, sales summary, low stock, unsold items. |
| **Catalog RAG** | `POST /api/ai/catalog/upload` · `/ask` · `/rebuild-embeddings` | Upload a PDF catalog → chunk → embed → answer questions with hybrid (vector + keyword) retrieval. |
| **Unified Orchestrator** | `POST /api/ai/ask` | One endpoint that routes a message to the right tool above. |
| **Product index** | `POST /api/ai/products/reindex` · `GET /index-status` | Build / inspect the semantic embedding index over the catalog. |

See **[AI_FEATURES.md](AI_FEATURES.md)** for the full architecture, flows, and testing guide.

## Repository layout

```
Dokkana/
├── backend/          ASP.NET Core (.NET 10) — N-Tier Web API
│   ├── Dokkana.API/        Presentation: controllers, auth, DI, pipeline
│   ├── Dokkana.BLL/        Business logic: managers, DTOs, AI/ subsystem
│   ├── Dokkana.DAL/        Data access: EF Core, models, repositories, UoW
│   ├── Dokkana.Common/     Shared result wrappers (GeneralResult, PagedResult)
│   └── Dokkana.slnx        Solution file
│
└── frontend/         Angular 20 SPA (shopper UI · store portal · admin)
    └── src/app/{core, shared, features}/
```

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core, SQL Server, Identity + JWT, SignalR
- **AI:** **OpenRouter** (LLM chat + embeddings) via a multi-agent pipeline
- **RAG:** embeddings stored as JSON in SQL Server, cosine-similarity retrieval, PdfPig for PDF text
- **Web search:** Serper (optional)
- **Frontend:** Angular 20 (standalone components, signals)

## Roles

`Admin` · `StoreOwner` · `Shopper` — seeded automatically on first run.

## Getting started

### Backend
```bash
cd backend/Dokkana.API

# AI keys (the app runs without them, but falls back to keyword-only search).
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-v1-..."   # from openrouter.ai (free tier)
dotnet user-secrets set "Serper:ApiKey"     "..."            # optional — enables web results

dotnet run                                                    # http://localhost:5034
```
- Development uses **SQL Server LocalDB** (`DokkanaDb`) and applies migrations + seeds on startup.
- To switch to a zero-setup in-memory DB, set `"Database:Provider": "InMemory"` in `appsettings.Development.json` (data won't persist).
- After the first run with an OpenRouter key, build the semantic index once:
  `POST http://localhost:5034/api/ai/products/reindex` with `{ "forceRebuild": true }`.

### Frontend
```bash
cd frontend
npm install
npm start            # http://localhost:4200
```

Test logins: admin `admin@dokkana.com` / `Admin123!`, store owner `store@dokkana.com` / `Store123!`.

## Without an API key

The whole app still runs. AI search falls back to **keyword search** over the real catalog
(stop-word filtered), Copilot/Catalog fall back to keyword retrieval. Add an OpenRouter key
to turn on real language understanding, semantic search, and AI-written answers.

## Team conventions

- Branch per feature; PR into `main`. Don't push directly to `main`.
- Keep `Dokkana.BLL/DTOs` (backend) and `frontend/src/app/core/models` in sync.
- Never commit secrets — use `dotnet user-secrets` (backend).
