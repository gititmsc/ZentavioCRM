namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Per-request "which tenant is this?" state. Populated by TenantResolutionMiddleware
    /// (Api layer) early in the pipeline, then read when AppDbContext is constructed so every
    /// repository transparently operates against the correct tenant's database. Registered
    /// Scoped so one instance is shared across everything resolved during a single request.
    /// </summary>
    public interface ITenantContext
    {
        Guid? TenantId { get; }

        string? Subdomain { get; }

        /// <summary>Full ADO.NET connection string for this tenant's database. Null until resolved.</summary>
        string? ConnectionString { get; }

        bool IsResolved { get; }

        void Resolve(Guid tenantId, string subdomain, string connectionString);
    }
}
