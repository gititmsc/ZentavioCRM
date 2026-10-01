namespace ZentavioCRM.Core.DTOs.Leads
{
    /// <summary>Full set of tenant-configurable lead-scoring weights — see LeadScoringSettings.cs for what each field means. Used for both reading the current settings and submitting an update (same shape, no separate request type needed since every field is always supplied by the settings form).</summary>
    public class LeadScoringSettingsDto
    {
        public int EmailPresentPoints { get; set; }

        public int MobilePresentPoints { get; set; }

        public int IndustryPresentPoints { get; set; }

        public int AssignedPoints { get; set; }

        public decimal ExpectedValueHighThreshold { get; set; }

        public int ExpectedValueHighPoints { get; set; }

        public decimal ExpectedValueMediumThreshold { get; set; }

        public int ExpectedValueMediumPoints { get; set; }

        public int ExpectedValueLowPoints { get; set; }

        public int SourceReferralPoints { get; set; }

        public int SourceWarmChannelPoints { get; set; }

        public int UrgentTimelinePoints { get; set; }

        public int PointsPerCompletedActivity { get; set; }

        public int EngagementMaxPoints { get; set; }

        public int MaxScore { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
