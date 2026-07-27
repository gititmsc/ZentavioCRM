namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A single human-readable history entry for a significant change to a CRM record
    /// (created, updated, status/stage changed, assigned, converted, deleted). Intentionally
    /// stores a plain-English summary rather than a full field-by-field diff — simpler to produce
    /// and to read, at the cost of not being queryable at the individual-field level.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }

        /// <summary>"Lead", "Opportunity", "Customer", etc.</summary>
        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        /// <summary>"Created", "Updated", "Deleted", "StatusChanged", "StageChanged", "Assigned", "Converted".</summary>
        public string Action { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public Guid? PerformedByUserId { get; set; }

        public User? PerformedByUser { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
