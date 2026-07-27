namespace ZentavioCRM.Core.DTOs.Audit
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string? PerformedByUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
