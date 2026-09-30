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

        public PaymentStatus PaymentStatus { get; set; }

        public decimal? BillingAmount { get; set; }

        public string? BillingCurrency { get; set; }

        public BillingCycle? BillingCycle { get; set; }

        public DateTime? NextDueDateUtc { get; set; }
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

    /// <summary>One tenant/metric pair at or above the "near limit" threshold — the "Tenants
    /// nearing/at limits" Dashboard widget's row shape. See
    /// ITenantUsageService.GetTenantsNearLimitsAsync.</summary>
    public class TenantUsageAlertDto
    {
        public Guid TenantId { get; set; }

        public string TenantName { get; set; } = string.Empty;

        /// <summary>"Users", "Records" or "Storage".</summary>
        public string Metric { get; set; } = string.Empty;

        public decimal Current { get; set; }

        public decimal Max { get; set; }

        /// <summary>0-100.</summary>
        public decimal PercentUsed { get; set; }

        public bool AtLimit { get; set; }
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

    /// <summary>Edits the tenant registry's own display fields after provisioning. Note:
    /// <see cref="AdminEmail"/> here is the denormalized directory copy only (see
    /// <see cref="Entities.Platform.Tenant.AdminEmail"/>) — changing it does NOT change what the
    /// tenant's actual admin user signs in with in their own database; that stays untouched.</summary>
    public class UpdateTenantMetadataRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string AdminEmail { get; set; } = string.Empty;
    }

    /// <summary>Updates a tenant's manually-tracked billing fields directly (no payment recorded).
    /// All fields optional/PATCH-style — only supplied fields are changed. Setting
    /// <see cref="PaymentStatus"/> to Overdue on a currently-Active tenant automatically suspends
    /// it; see <see cref="Interfaces.ITenantBillingService.UpdateBillingAsync"/>.</summary>
    public class UpdateTenantBillingRequest
    {
        public PaymentStatus? PaymentStatus { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? BillingAmount { get; set; }

        [MaxLength(3)]
        public string? BillingCurrency { get; set; }

        public BillingCycle? BillingCycle { get; set; }

        public DateTime? NextDueDateUtc { get; set; }
    }

    /// <summary>Records one payment-history entry. There is no payment gateway involved — this is
    /// purely "a platform admin says this payment happened." Recording one sets the tenant's
    /// PaymentStatus to Paid and advances NextDueDateUtc by one BillingCycle when set.</summary>
    public class RecordTenantPaymentRequest
    {
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string? Currency { get; set; }

        /// <summary>Defaults to now (UTC) if omitted.</summary>
        public DateTime? PaidAtUtc { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }
    }

    public class TenantPaymentDto
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public DateTime PaidAtUtc { get; set; }

        public string? Note { get; set; }

        /// <summary>"System" if the recording admin's account was later deleted.</summary>
        public string RecordedByAdminEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }

    public class CreateTenantNoteRequest
    {
        [Required, MaxLength(2000)]
        public string Note { get; set; } = string.Empty;
    }

    public class TenantNoteDto
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Note { get; set; } = string.Empty;

        /// <summary>"System" if the authoring admin's account was later deleted.</summary>
        public string CreatedByAdminEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
