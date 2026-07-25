using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="ITenantContext"/>
    /// <remarks>Registered Scoped — one instance per request, written once by
    /// TenantResolutionMiddleware (Api layer) and read whenever AppDbContext is constructed.</remarks>
    public class TenantContext : ITenantContext
    {
        public Guid? TenantId { get; private set; }

        public string? Subdomain { get; private set; }

        public string? ConnectionString { get; private set; }

        public bool IsResolved => ConnectionString is not null;

        public void Resolve(Guid tenantId, string subdomain, string connectionString)
        {
            TenantId = tenantId;
            Subdomain = subdomain;
            ConnectionString = connectionString;
        }
    }
}
