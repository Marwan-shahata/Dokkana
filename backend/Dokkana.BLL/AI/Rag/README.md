# RAG pipeline

Services for turning uploaded catalog PDFs into searchable vectors. To be added:

- **PdfExtractorService** — PdfPig: PDF → list of (page, text). ⚠️ Test Arabic early.
- **TextChunkerService** — SK TextChunker: text → overlapping chunks.
- **EmbeddingService** — OpenAI `text-embedding-3-small` → float[1536].
