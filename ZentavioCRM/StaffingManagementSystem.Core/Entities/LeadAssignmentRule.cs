namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A round-robin assignment pool for incoming Leads. When <see cref="TerritoryId"/> is set, the
    /// rule applies only to leads whose <see cref="Lead.TerritoryId"/> matches; a single rule with a
    /// null <see cref="TerritoryId"/> acts as the tenant-wide fallback for leads with no territory
    /// (or no matching territory-specific rule). Eligible users are <see cref="LeadAssignmentRuleUser"/>
    /// rows; <see cref="LastAssignedUserId"/> is the round-robin cursor, advanced one position (wrapping)
    /// each time this rule assigns a lead.
    /// </summary>
    public class LeadAssignmentRule
    {
        public Guid Id { get; set; }

        /// <summary>Null means this is the tenant-wide fallback rule — at most one such row should be active at a time (enforced in the service layer, not the database).</summary>
        public Guid? TerritoryId { get; set; }

        public Territory? Territory { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>The user the round-robin cursor last landed on for this rule — the next assignment picks the following eligible user (by Id order), wrapping to the first if this was the last.</summary>
        public Guid? LastAssignedUserId { get; set; }

        public User? LastAssignedUser { get; set; }

        public DateTime? LastAssignedAtUtc { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<LeadAssignmentRuleUser> EligibleUsers { get; set; } = new List<LeadAssignmentRuleUser>();
    }
}
