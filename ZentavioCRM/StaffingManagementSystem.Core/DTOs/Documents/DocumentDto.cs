namespace ZentavioCRM.Core.DTOs.Documents
{
    /// <summary>Metadata only — never carries the file bytes, so listing a record's attachments stays cheap.</summary>
    public class DocumentDto
    {
        public Guid Id { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public string? UploadedByUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
