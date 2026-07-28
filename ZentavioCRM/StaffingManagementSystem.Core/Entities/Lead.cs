using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// An unqualified inquiry at the top of the sales funnel. Progresses through
    /// <see cref="LeadStatus"/> until it is converted into a <see cref="Customer"/> or lost.
    /// </summary>
    public class Lead
    {
        public Guid Id { get; set; }

        /// <summary>Human-friendly sequential number, e.g. "LEAD-000123".</summary>
        public string LeadNumber { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ContactName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? Industry { get; set; }

        public LeadSource Source { get; set; } = LeadSource.ManualEntry;

        public string? Campaign { get; set; }

        public decimal? Budget { get; set; }

        public string? Timeline { get; set; }

        public decimal? ExpectedValue { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        public string? Territory { get; set; }

        public LeadStatus Status { get; set; } = LeadStatus.New;

        public int? LeadScore { get; set; }

        /// <summary>Reserved for the future AI scoring engine described in the SRS.</summary>
        public int? AiScore { get; set; }

        public string? Notes { get; set; }

        /// <summary>When to next follow up with this lead — one of the most-used fields in a real lead pipeline.</summary>
        public DateTime? NextFollowUpDate { get; set; }

        /// <summary>Set once a reminder notification has been sent for the current <see cref="NextFollowUpDate"/>, so it isn't re-sent on every poll. Cleared whenever NextFollowUpDate changes.</summary>
        public DateTime? FollowUpReminderSentAtUtc { get; set; }

        public string? LostReason { get; set; }

        public Guid? ConvertedCustomerId { get; set; }

        public Customer? ConvertedCustomer { get; set; }

        public DateTime? ConvertedAtUtc { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
