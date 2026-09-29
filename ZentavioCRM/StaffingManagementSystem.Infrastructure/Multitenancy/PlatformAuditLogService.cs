using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="IPlatformAuditLogService"/>
    public class PlatformAuditLogService : IPlatformAuditLogService
    {
        private readonly PlatformDbContext _platformDb;

        public PlatformAuditLogService(PlatformDbContext platformDb)
        {
            _platformDb = platformDb;
        }

        public async Task LogAsync(Guid? platformAdminId, string action, string summary, Guid? tenantId = null)
        {
            try
            {
                _platformDb.PlatformAuditLogs.Add(new PlatformAuditLog
                {
                    PlatformAdminId = platformAdminId,
                    Action = action,
                    TenantId = tenantId,
                    Summary = summary,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                await _platformDb.SaveChangesAsync();
            }
            catch
            {
                // Audit logging is best-effort — the action being logged (login, suspend, ...) has
                // already succeeded by the time this runs, so a logging failure here should never
                // surface as a failure of that action. Same reasoning as the best-effort cleanup in
                // TenantProvisioningService.ProvisionAsync.
            }
        }

        public async Task<IReadOnlyList<PlatformAuditLogDto>> GetAllAsync(Guid? tenantId = null)
        {
            var query = _platformDb.PlatformAuditLogs
                .Include(l => l.PlatformAdmin)
                .Include(l => l.Tenant)
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(l => l.TenantId == tenantId.Value);
            }

            return await query
                .OrderByDescending(l => l.CreatedAtUtc)
                .Select(l => new PlatformAuditLogDto
                {
                    Id = l.Id,
                    PlatformAdminId = l.PlatformAdminId,
                    PlatformAdminEmail = l.PlatformAdmin != null ? l.PlatformAdmin.Email : null,
                    Action = l.Action,
                    TenantId = l.TenantId,
                    TenantName = l.Tenant != null ? l.Tenant.Name : null,
                    Summary = l.Summary,
                    CreatedAtUtc = l.CreatedAtUtc,
                })
                .ToListAsync();
        }
    }
}
