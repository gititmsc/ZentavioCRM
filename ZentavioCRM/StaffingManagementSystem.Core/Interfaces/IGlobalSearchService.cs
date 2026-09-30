using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Backs the Platform Admin console's topbar search box — finds a tenant (by name, subdomain
    /// or admin email) or a platform admin (by name or email) by a single free-text term. Both
    /// Tenants and PlatformAdmins live in the shared Platform database, so unlike most Platform-side
    /// services this needs no cross-tenant-database connection at all.
    /// </summary>
    public interface IGlobalSearchService
    {
        /// <summary>Empty/whitespace <paramref name="term"/> returns no results — this isn't a
        /// browse-everything listing, just search.</summary>
        Task<IReadOnlyList<GlobalSearchResultDto>> SearchAsync(string term);
    }
}
