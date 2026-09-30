using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="IGlobalSearchService"/>
    public class GlobalSearchService : IGlobalSearchService
    {
        private readonly PlatformDbContext _platformDb;

        /// <summary>Cap per entity type — this is a "jump to the thing you're thinking of" box, not
        /// a full results page, so a long tail of matches isn't useful here.</summary>
        private const int MaxResultsPerType = 10;

        public GlobalSearchService(PlatformDbContext platformDb)
        {
            _platformDb = platformDb;
        }

        public async Task<IReadOnlyList<GlobalSearchResultDto>> SearchAsync(string term)
        {
            var trimmed = term?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return [];
            }

            var tenants = await _platformDb.Tenants
                .Where(t => EF.Functions.Like(t.Name, $"%{trimmed}%")
                    || EF.Functions.Like(t.Subdomain, $"%{trimmed}%")
                    || EF.Functions.Like(t.AdminEmail, $"%{trimmed}%"))
                .OrderBy(t => t.Name)
                .Take(MaxResultsPerType)
                .Select(t => new GlobalSearchResultDto
                {
                    Type = "Tenant",
                    Id = t.Id,
                    Label = t.Name,
                    SubLabel = t.Subdomain,
                })
                .ToListAsync();

            var admins = await _platformDb.PlatformAdmins
                .Where(a => EF.Functions.Like(a.FirstName, $"%{trimmed}%")
                    || EF.Functions.Like(a.LastName, $"%{trimmed}%")
                    || EF.Functions.Like(a.Email, $"%{trimmed}%"))
                .OrderBy(a => a.Email)
                .Take(MaxResultsPerType)
                .Select(a => new GlobalSearchResultDto
                {
                    Type = "PlatformAdmin",
                    Id = a.Id,
                    Label = (a.FirstName + " " + a.LastName).Trim(),
                    SubLabel = a.Email,
                })
                .ToListAsync();

            return [.. tenants, .. admins];
        }
    }
}
