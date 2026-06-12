# Agentic AI Study Plan — Dokkana Project

## Phase 1: Foundations (Week 1)

### 1.1 Understand Agentic AI Concepts

| Concept | Why for Dokkana |
|---|---|
| LLM → Agent paradigm | Moving from simple Q&A to tool-using agents |
| ReAct pattern (Reason + Act) | Agent decides when to search products vs answer from knowledge |
| Tool/Function calling | LLM calls `ProductSearchPlugin` instead of guessing |
| System prompts & guardrails | `AgentPrompts.cs` controls behavior |
| Hallucination & grounding | RAG grounds answers in real product data |

**Resources:**
- Read: OpenAI function calling docs
- Read: "Building effective agents" (Anthropic)
- Watch: Semantic Kernel 101 videos

### 1.2 Understand the Existing Stack
- .NET 10, ASP.NET Core, EF Core, SQL Server
- Identity with JWT (roles: Shopper, StoreOwner, Admin)
- Clean architecture: API → BLL → DAL
- Current AI stub in `DokkanaAgent.cs`

---

## Phase 2: Semantic Kernel (Week 2)

### 2.1 Core SK Concepts

| Concept | Application |
|---|---|
| `Kernel` | Central orchestrator — connects LLM + plugins |
| `ChatCompletionService` | OpenAI `gpt-4o-mini` |
| `KernelFunction` / Plugin | `ProductSearchPlugin`, `CatalogRagPlugin` |
| `FunctionChoiceBehavior` | Auto — let LLM decide when to call tools |
| `PromptTemplateConfig` | System prompt from `AgentPrompts.cs` |
| `TextChunker` | Split catalog PDFs into overlapping chunks |

### 2.2 Hands-on
1. Add NuGet: `Microsoft.SemanticKernel`, `Microsoft.SemanticKernel.Connectors.OpenAI`
2. Wire up `Kernel` in DI with OpenAI config (`gpt-4o-mini`, `text-embedding-3-small`)
3. Replace the stub in `DokkanaAgent.AskAsync()` with a real `Kernel` invocation
4. Test: `POST /api/ai/search` returns a real LLM response

### 2.3 What to Build First (Minimal Viable Agent)

```
User: "red dress under 500 EGP"
  → Agent receives query
  → Calls ProductSearchPlugin(query)
  → Plugin returns matching products from SQL
  → LLM formats natural-language answer
  → Returns AiSearchResponseDTO
```

---

## Phase 3: Plugins (Week 3)

### 3.1 ProductSearchPlugin (SQL + Semantic)

| Step | Implementation |
|---|---|
| Parse NL → filters | `ProductFilterDTO { Category?, MinPrice?, MaxPrice?, Color?, StoreId? }` |
| Query SQL Server | `ProductManager.SearchAsync(filters)` |
| Return structured results | `List<ProductDTO>` — name, price, store, image |

**Key challenge:** Extracting structured filters from natural language. Options:
- Prompt LLM to output JSON `{"category": "dress", "maxPrice": 500}`
- Use SK `JsonSchema` function parameters

### 3.2 CatalogRagPlugin (Vector Search)

| Component | What it does |
|---|---|
| `PdfExtractorService` | PDF → text chunks (via UglyToad.PdfPig) |
| `TextChunkerService` | SK `TextChunker.SplitPlainTextParagraphs()` |
| `EmbeddingService` | OpenAI `text-embedding-3-small` → 1536d vector |
| Vector store | Supabase pgvector (store/retrieve embeddings) |

**RAG Flow:**

```
Query → embed → pgvector similarity search → top 5 chunks → LLM answers with context
```

### 3.3 BusinessCopilotPlugin (Analytics)
- Store-owner dashboard Q&A
- "What sold best last month?" → query Orders table
- Restricted to `StoreOwner` role

---

## Phase 4: Background Ingestion (Week 4)

### 4.1 Async PDF Processing

| Piece | Technology |
|---|---|
| Queue | `System.Threading.Channels.Channel<T>` |
| Background worker | `IHostedService` |
| PDF parsing | UglyToad.PdfPig |
| Chunking | SK `TextChunker` |
| Embedding | OpenAI API |
| Storage | pgvector upsert |

**Flow:**

```
Upload PDF → enqueue → worker picks up → extract → chunk → embed → save to pgvector
```

### 4.2 Monitoring
- Logging ingestion progress
- Handle failures (retry logic)
- Status endpoint for frontend

---

## Phase 5: Advanced (Week 5+)

### 5.1 Multi-turn Conversations
- Maintain chat history in session
- Agent remembers previous context (e.g., "the red one" refers to prior mention)

### 5.2 Arabic Support
- Test with Arabic queries (RTL text, tokenization)
- `AgentPrompts.cs` already has Arabic instruction
- Ensure PdfPig handles Arabic PDFs

### 5.3 Cost Optimization
- Cache embeddings
- Limit history length
- Use gpt-4o-mini (cheaper) for most calls

### 5.4 Streaming Responses
- Server-Sent Events (SSE) for typing-effect in frontend

---

## Architecture Diagram (Target)

```
Frontend (Angular)
    │ POST /api/ai/search { query }
    ▼
AiSearchController
    │
    ▼
IDokkanaAgent.AskAsync()
    │
    ▼
Kernel (Semantic Kernel)
    │
    ├── ChatCompletionService (gpt-4o-mini)
    ├── ProductSearchPlugin ──→ ProductManager ──→ SQL Server
    ├── CatalogRagPlugin   ──→ EmbeddingService ──→ pgvector
    └── BusinessCopilotPlugin ──→ OrderManager ──→ SQL Server
```

---

## NuGet Packages to Add

| Package | Phase |
|---|---|
| `Microsoft.SemanticKernel` | 2 |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | 2 |
| `UglyToad.PdfPig` | 4 |
| `Pomelo.EntityFrameworkCore.MySql` (if pgvector) | 3 |

---

## Milestone Checklist

- [ ] Phase 1: Agentic AI concepts understood
- [ ] Phase 2: SK kernel wired, stub replaced, first AI response
- [ ] Phase 3.1: ProductSearchPlugin filters + returns real products
- [ ] Phase 3.2: RAG pipeline — PDF → chunk → embed → search → answer
- [ ] Phase 3.3: BusinessCopilot analytics queries
- [ ] Phase 4: Background ingestion works end-to-end
- [ ] Phase 5: Streaming, Arabic, multi-turn
