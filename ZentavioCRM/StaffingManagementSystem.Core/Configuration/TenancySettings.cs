namespace ZentavioCRM.Core.Configuration
{
    /// <summary>
    /// Strongly typed binding of the "Tenancy" configuration section — everything needed to
    /// build a per-tenant connection string and provision new tenant databases.
    /// </summary>
    public class TenancySettings
    {
        public const string SectionName = "Tenancy";

        /// <summary>Server + credentials only, no Initial Catalog — combined with each Tenant's
        /// DatabaseName to build the actual per-request connection string.</summary>
        public string SqlServerHostConnectionString { get; set; } = string.Empty;

        public string TenantDatabaseNamePrefix { get; set; } = "ZentavioCRM_Tenant_";

        /// <summary>Header clients can send to select a tenant explicitly (used by the frontend,
        /// and the only option in local dev where subdomains aren't practical).</summary>
        public string TenantHeaderName { get; set; } = "X-Tenant";

        /// <summary>Root domain suffix stripped when resolving a tenant from the Host header,
        /// e.g. "zentaviocrm.com" so "acme.zentaviocrm.com" resolves to subdomain "acme".</summary>
        public string RootDomain { get; set; } = "zentaviocrm.com";

        /// <summary>
        /// Named entry under ConnectionStrings to fall back to when no tenant could be resolved
        /// from the request (no header, no matching subdomain) — keeps plain http://localhost
        /// working for local development without requiring every request to carry a tenant.
        /// Leave empty to require tenant resolution on every request (production default).
        /// </summary>
        public string? DefaultTenantConnectionStringName { get; set; }
    }
}
