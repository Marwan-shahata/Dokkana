# AI Plugins (Semantic Kernel tools)

Each plugin is a "tool" the `DokkanaAgent` can call. To be added in Sprint 2–3:

- **ProductSearchPlugin** — turns a natural-language query into a `ProductFilterDTO`
  and calls the existing `ProductManager` (SQL Server).
- **CatalogRagPlugin** — embeds the question, retrieves similar chunks from the
  vector store (Supabase pgvector), and returns a grounded answer.
- **BusinessCopilotPlugin** — answers store-owner questions ("what sold most this
  month?", "what's low on stock?") by querying the Orders / Product tables.
