// Types for the merchant AI tools: Business Copilot + Catalog RAG.
// Mirror the backend DTOs in Dokkana.BLL/DTOs/AI/{Copilot,Catalog}.

// ── Business Copilot ───────────────────────────────────────────────────────
export interface BusinessIntent {
  intent: string;            // top_selling_products | sales_summary | low_stock_products | unsold_products
  period: string;            // this_week | this_month | all_time ...
  limit: number;
  lowStockThreshold: number;
}

// `data` shape depends on intent; the UI renders it generically as a table.
export interface BusinessCopilotResponse {
  question: string;
  intent: BusinessIntent;
  answer: string;
  data: unknown;
}

// ── Catalog RAG ────────────────────────────────────────────────────────────
export interface CatalogUploadResponse {
  catalogDocumentId: number;
  storeId: number;
  fileName: string;
  status: string;            // Ready | Failed | Processing
  chunksCount: number;
  message: string;
}

export interface RetrievedCatalogChunk {
  chunkId: number;
  catalogDocumentId: number;
  chunkIndex: number;
  content: string;
  vectorScore: number;
  keywordScore: number;
  latestCatalogScore: number;
  finalScore: number;
}

export interface CatalogQuestionResponse {
  storeId: number;
  question: string;
  answer: string;
  retrievalMode: string;     // hybrid | vector | keyword | fallback
  usedChunks: string[];
  retrievedChunks: RetrievedCatalogChunk[];
}
