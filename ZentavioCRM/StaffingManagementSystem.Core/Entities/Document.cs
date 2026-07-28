namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A file attached to any CRM record (contract, signed agreement, proposal...). Content is
    /// stored as a blob directly in the tenant database rather than on disk or in cloud object
    /// storage — simplest option for a one-database-per-tenant architecture with no existing
    /// blob storage integration, and it travels automatically with tenant backup/restore.
    /// </summary>
    public class Document
    {
        public Guid Id { get; set; }

        /// <summary>"Customer", "Opportunity", etc.</summary>
        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeBytes { get; set; }

        public byte[] Content { get; set; } = [];

        public Guid? UploadedByUserId { get; set; }

        public User? UploadedByUser { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
