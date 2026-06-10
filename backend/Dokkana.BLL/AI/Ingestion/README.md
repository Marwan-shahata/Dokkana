# Background ingestion

Processes catalog PDFs off the request thread. To be added:

- **ICatalogIngestionQueue / CatalogIngestionQueue** — in-memory `Channel<T>` queue.
- **CatalogIngestionWorker** — `IHostedService` that dequeues a job and runs the
  RAG pipeline: extract → chunk → embed → save to the vector store.
