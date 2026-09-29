using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Platform
{
    public class TenantDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Subdomain { get; set; } = string.Empty;

        public string DatabaseName { get; set; } = string.Empty;

        public TenantStatus Status { get; set; }

        public string AdminEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ActivatedAtUtc { get; set; }

        public PlanTier PlanTier { get; set; }

        public int MaxUsers { get; set; }

        public int MaxStorageMB { get; set; }

        public int MaxRecords { get; set; }
    }

    /// <summary>Change a tenant's plan tier. Omitted limit fields fall back to
    /// <see cref="Configuration.PlanTierDefaults"/> for the new tier; supply any of them
    /// explicitly to override just that one limit for this tenant.</summary>
    public class UpdateTenantPlanRequest
    {
        [Required]
        public PlanTier PlanTier { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxUsers { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxStorageMB { get; set; }

        [Range(1, int.MaxValue)]
        public int? MaxRecords { get; set; }
    }

    /// <summary>Current usage vs. plan limits for a tenant, computed live against that tenant's
    /// own database — not cached, since a platform admin checking this page wants the real
    /// number, not a stale one. See ITenantUsageService.</summary>
    public class TenantUsageDto
    {
        public Guid TenantId { get; set; }

        public PlanTier PlanTier { get; set; }

        public int UserCount { get; set; }

        public int MaxUsers { get; set; }

        /// <summary>Leads + Customers + Opportunities + Quotations + SalesOrders combined.</summary>
        public int RecordCount { get; set; }

        public int MaxRecords { get; set; }

        public decimal DatabaseSizeMB { get; set; }

        public int MaxStorageMB { get; set; }

        /// <summary>Most recent LastLoginAtUtc across the tenant's Users. Null if no user has
        /// logged in yet.</summary>
        public DateTime? LastActivityAtUtc { get; set; }
    }

    /// <summary>Just the limits, for the tenant-side enforcement check (see IPlanLimitService) —
    /// deliberately not the full TenantDto since that's Platform-admin-only data the tenant-side
    /// Services layer has no other business seeing.</summary>
    public class TenantLimitsDto
    {
        public Guid TenantId { get; set; }

        public PlanTier PlanTier { get; set; }

        public int MaxUsers { get; set; }

        public int MaxStorageMB { get; set; }

        public int MaxRecords { get; set; }
    }

    /// <summary>Optional context for the audit log entry — not a business rule, just a record of why.</summary>
    public class SuspendTenantRequest
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>See <see cref="SuspendTenantRequest"/>.</summary>
    public class StopTenantRequest
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
