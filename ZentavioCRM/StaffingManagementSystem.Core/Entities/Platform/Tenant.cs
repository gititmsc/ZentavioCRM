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
    }
}
