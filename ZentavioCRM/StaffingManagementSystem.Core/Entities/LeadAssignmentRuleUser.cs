namespace ZentavioCRM.Core.Entities
{
    /// <summary>Join row: one user eligible for round-robin assignment under a <see cref="LeadAssignmentRule"/>. Composite key, no surrogate Id — same convention as <see cref="LeadTag"/>/<see cref="CustomerTag"/>/<see cref="RolePermission"/>.</summary>
    public class LeadAssignmentRuleUser
    {
        public Guid RuleId { get; set; }

        public LeadAssignmentRule? Rule { get; set; }

        public Guid UserId { get; set; }

        public User? User { get; set; }
    }
}
