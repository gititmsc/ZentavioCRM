namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// Tenant-configurable weights for <c>LeadService.ComputeLeadScore</c>. Exactly one row exists
    /// per tenant database (see <see cref="Common.SeedIds.LeadScoringSettingsId"/> — fixed Id, "singleton
    /// row" pattern), created with these exact defaults the first time it's read if the row is somehow
    /// missing (e.g. a tenant provisioned before this feature existed, before 007_LeadScoringSettings.sql
    /// runs). The defaults below intentionally reproduce the original hardcoded formula byte-for-byte,
    /// so turning this feature on changes nothing until an admin actually edits a weight.
    /// </summary>
    public class LeadScoringSettings
    {
        public Guid Id { get; set; }

        /// <summary>Points for Email being present.</summary>
        public int EmailPresentPoints { get; set; } = 15;

        /// <summary>Points for Mobile being present.</summary>
        public int MobilePresentPoints { get; set; } = 15;

        /// <summary>Points for Industry being present.</summary>
        public int IndustryPresentPoints { get; set; } = 10;

        /// <summary>Points for the lead already being assigned to a user.</summary>
        public int AssignedPoints { get; set; } = 10;

        /// <summary>ExpectedValue at/above this gets <see cref="ExpectedValueHighPoints"/>.</summary>
        public decimal ExpectedValueHighThreshold { get; set; } = 50000m;

        public int ExpectedValueHighPoints { get; set; } = 25;

        /// <summary>ExpectedValue at/above this (but below the high threshold) gets <see cref="ExpectedValueMediumPoints"/>.</summary>
        public decimal ExpectedValueMediumThreshold { get; set; } = 10000m;

        public int ExpectedValueMediumPoints { get; set; } = 15;

        /// <summary>ExpectedValue above zero (but below the medium threshold) gets this.</summary>
        public int ExpectedValueLowPoints { get; set; } = 5;

        /// <summary>Points for Source == Referral.</summary>
        public int SourceReferralPoints { get; set; } = 20;

        /// <summary>Points for Source in {LinkedIn, Website, LandingPage, Exhibition}.</summary>
        public int SourceWarmChannelPoints { get; set; } = 10;

        /// <summary>Points when Timeline contains "month", "quarter", "immediate" or "asap" (case-insensitive).</summary>
        public int UrgentTimelinePoints { get; set; } = 5;

        /// <summary>
        /// New factor (not in the original hardcoded formula): points per completed Activity logged
        /// against the lead, reflecting real engagement rather than just form-field completeness.
        /// </summary>
        public int PointsPerCompletedActivity { get; set; } = 2;

        /// <summary>Cap on the total engagement contribution, so a lead with dozens of logged calls doesn't dwarf every other factor.</summary>
        public int EngagementMaxPoints { get; set; } = 10;

        /// <summary>Overall cap on the computed score — matches the original hardcoded 0-100 range.</summary>
        public int MaxScore { get; set; } = 100;

        public Guid? UpdatedByUserId { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
