using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities.Platform
{
    /// <summary>
    /// A row in the Platform (master) database's tenant registry — one per customer company.
    /// Each Tenant points at its own dedicated database; no tenant's application data (Users,
    /// Leads, Customers...) ever lives in this database.
    /// </summary>
    public class Tenant
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Lowercase, URL-safe — resolves "acme.zentaviocrm.com" to this tenant.</summary>
        public string Subdomain { get; set; } = string.Empty;

        /// <summary>Physical database name, e.g. "ZentavioCRM_Tenant_acme". Combined with the shared
        /// SQL Server host connection string (Tenancy:SqlServerHostConnectionString) at request time —
        /// the full connection string is never stored so credential rotation doesn't require a data migration.</summary>
        public string DatabaseName { get; set; } = string.Empty;

        public TenantStatus Status { get; set; } = TenantStatus.Provisioning;

        /// <summary>Denormalized for quick display in the platform admin list — the tenant's own Users table is the source of truth.</summary>
        public string AdminEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ActivatedAtUtc { get; set; }

        public PlanTier PlanTier { get; set; } = PlanTier.Trial;

        /// <summary>Usage limits, seeded from <see cref="Configuration.PlanTierDefaults"/> for
        /// <see cref="PlanTier"/> at provision/tier-change time but independently editable per
        /// tenant afterward (see PATCH /api/platform/tenants/{id}/plan).</summary>
        public int MaxUsers { get; set; }

        public int MaxStorageMB { get; set; }

        public int MaxRecords { get; set; }

        /// <summary>Manually-tracked billing status — no payment gateway is involved anywhere in
        /// this system. Defaults to Unpaid for a newly-provisioned tenant. Setting this to
        /// <see cref="PaymentStatus.Overdue"/> automatically suspends the tenant (see
        /// <see cref="Interfaces.ITenantBillingService"/>); setting it back to Paid does not
        /// auto-reactivate — that stays a separate, deliberate admin action.</summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        /// <summary>What this tenant is being charged, in <see cref="BillingCurrency"/>. Purely
        /// informational — nothing here triggers an actual charge.</summary>
        public decimal? BillingAmount { get; set; }

        /// <summary>ISO 4217 code, e.g. "USD". Defaults to "USD" when a BillingAmount is first set.</summary>
        public string? BillingCurrency { get; set; }

        public BillingCycle? BillingCycle { get; set; }

        /// <summary>When the next payment is expected. Advanced automatically by one billing
        /// cycle each time a payment is recorded (see ITenantBillingService.RecordPaymentAsync);
        /// otherwise editable directly.</summary>
        public DateTime? NextDueDateUtc { get; set; }
    }
}
