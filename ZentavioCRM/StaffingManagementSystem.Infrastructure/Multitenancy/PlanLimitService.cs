using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="IPlanLimitService"/>
    public class PlanLimitService : IPlanLimitService
    {
        private readonly PlatformDbContext _platformDb;

        public PlanLimitService(PlatformDbContext platformDb)
        {
            _platformDb = platformDb;
        }

        public Task<TenantLimitsDto?> GetLimitsAsync(Guid tenantId)
            => _platformDb.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => new TenantLimitsDto
                {
                    TenantId = t.Id,
                    PlanTier = t.PlanTier,
                    MaxUsers = t.MaxUsers,
                    MaxStorageMB = t.MaxStorageMB,
                    MaxRecords = t.MaxRecords,
                })
                .FirstOrDefaultAsync();
    }
}
