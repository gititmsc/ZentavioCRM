using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IAuditLogRepository"/>
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _dbContext;

        public AuditLogRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(AuditLog auditLog)
        {
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AuditLog>> GetForEntityAsync(string entityType, Guid entityId)
        {
            var logs = await _dbContext.AuditLogs
                .Include(a => a.PerformedByUser)
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync();

            return logs;
        }
    }
}
