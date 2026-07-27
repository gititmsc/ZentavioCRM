using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog auditLog);

        Task<IReadOnlyList<AuditLog>> GetForEntityAsync(string entityType, Guid entityId);
    }
}
