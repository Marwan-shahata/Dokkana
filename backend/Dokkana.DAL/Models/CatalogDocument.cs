namespace Dokkana.DAL.Models
{
    public enum CatalogStatus { Processing = 0, Ready = 1, Failed = 2 }

    // Metadata for an uploaded catalog PDF. The extracted/embedded chunks live in
    // the vector store (Supabase pgvector); this row just tracks the file + status.
    public class CatalogDocument
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public Store? Store { get; set; }
        public string FileName { get; set; } = string.Empty;
        public CatalogStatus Status { get; set; } = CatalogStatus.Processing;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
