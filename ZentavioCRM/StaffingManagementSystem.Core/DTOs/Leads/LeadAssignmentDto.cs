using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Leads
{
    /// <summary>Tenant-wide auto-assign on/off switch. Same shape for reading and updating — mirrors LeadScoringSettingsDto.</summary>
    public class LeadAssignmentSettingsDto
    {
        public bool AutoAssignEnabled { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class LeadAssignmentRuleUserDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;
    }

    public class LeadAssignmentRuleDto
    {
        public Guid Id { get; set; }

        /// <summary>Null means this is the tenant-wide fallback rule.</summary>
        public Guid? TerritoryId { get; set; }

        public string? TerritoryName { get; set; }

        public bool IsActive { get; set; }

        public List<LeadAssignmentRuleUserDto> EligibleUsers { get; set; } = [];

        public Guid? LastAssignedUserId { get; set; }

        public string? LastAssignedUserName { get; set; }

        public DateTime? LastAssignedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class SaveLeadAssignmentRuleRequest
    {
        /// <summary>Null creates/edits the tenant-wide fallback rule — at most one fallback rule (TerritoryId null) and one rule per territory may exist; the service rejects a save that would create a duplicate.</summary>
        public Guid? TerritoryId { get; set; }

        public bool IsActive { get; set; } = true;

        [MinLength(1, ErrorMessage = "Pick at least one eligible user.")]
        public List<Guid> EligibleUserIds { get; set; } = [];
    }
}
