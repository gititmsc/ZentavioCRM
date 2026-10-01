namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// Tenant-wide on/off switch for Lead auto-assignment. Exactly one row exists per tenant
    /// database (see <see cref="Common.SeedIds.LeadAssignmentSettingsId"/> — fixed Id, same
    /// "singleton row" pattern as <see cref="LeadScoringSettings"/>), created with these defaults
    /// the first time it's read if the row is somehow missing. Auto-assign defaults to off, so
    /// turning this feature on changes nothing for an existing tenant until an admin both enables
    /// it here and configures at least one <see cref="LeadAssignmentRule"/>.
    /// </summary>
    public class LeadAssignmentSettings
    {
        public Guid Id { get; set; }

        /// <summary>When true, a new Lead created without an explicit AssignedToUserId is routed via the matching active LeadAssignmentRule (by TerritoryId, falling back to the tenant-wide rule) instead of being left unassigned.</summary>
        public bool AutoAssignEnabled { get; set; }

        public Guid? UpdatedByUserId { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
