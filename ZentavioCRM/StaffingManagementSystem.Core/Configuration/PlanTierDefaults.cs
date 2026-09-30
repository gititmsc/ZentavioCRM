using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Configuration
{
    /// <summary>Default usage limits for a given <see cref="PlanTier"/>. Applied to a Tenant row
    /// when it's provisioned (defaults to <see cref="PlanTier.Trial"/> unless requested otherwise)
    /// or when a platform admin changes its tier without supplying explicit overrides. These are
    /// starting points, not hard-coded business rules — every value is stored per-tenant on the
    /// Tenant row itself, so any single tenant's limits can be adjusted independently of its tier
    /// (e.g. a Starter tenant granted extra seats as a one-off).</summary>
    public static class PlanTierDefaults
    {
        public readonly record struct Limits(int MaxUsers, int MaxStorageMB, int MaxRecords);

        /// <summary>How long a newly-provisioned Trial-tier tenant gets before
        /// TenantResolutionMiddleware auto-suspends it (see <see cref="Entities.Platform.Tenant.TrialEndsAtUtc"/>).
        /// Not applied retroactively to Trial tenants provisioned before this existed.</summary>
        public const int TrialLengthDays = 14;

        public static Limits For(PlanTier tier) => tier switch
        {
            PlanTier.Trial => new Limits(MaxUsers: 3, MaxStorageMB: 500, MaxRecords: 250),
            PlanTier.Starter => new Limits(MaxUsers: 10, MaxStorageMB: 5_000, MaxRecords: 5_000),
            PlanTier.Professional => new Limits(MaxUsers: 50, MaxStorageMB: 25_000, MaxRecords: 50_000),
            PlanTier.Enterprise => new Limits(MaxUsers: 500, MaxStorageMB: 250_000, MaxRecords: 500_000),
            _ => new Limits(MaxUsers: 3, MaxStorageMB: 500, MaxRecords: 250),
        };
    }
}
