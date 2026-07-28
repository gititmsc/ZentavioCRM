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

        /// <summary>utm_source — the referring site or platform (e.g. "google", "linkedin", "newsletter").</summary>
        public string? UtmSource { get; set; }

        /// <summary>utm_medium — the marketing medium (e.g. "cpc", "email", "social", "organic").</summary>
        public string? UtmMedium { get; set; }

        /// <summary>utm_campaign — the specific campaign identifier. Distinct from the freeform <see cref="Campaign"/> field, which is a human-readable label; this is the structured tracking parameter as it would appear in a URL.</summary>
        public string? UtmCampaign { get; set; }

        /// <summary>utm_term — paid search keyword, if applicable.</summary>
        public string? UtmTerm { get; set; }

        /// <summary>utm_content — differentiates similar content/links within the same campaign (e.g. A/B test variant, specific ad or CTA).</summary>
        public string? UtmContent { get; set; }

        public decimal? Budget { get; set; }

        public string? Timeline { get; set; }

        public decimal? ExpectedValue { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        /// <summary>Legacy free-text territory label — superseded by <see cref="TerritoryId"/> going forward. Kept as-is (no data migration, no removal) so existing CSV import/export never breaks.</summary>
        public string? Territory { get; set; }

        /// <summary>Structured territory reference (see <see cref="Entities.Territory"/>). Optional — a lead can still only carry the legacy free-text <see cref="Territory"/> label if this isn't set.</summary>
        public Guid? TerritoryId { get; set; }

        public Territory? TerritoryRef { get; set; }

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
