using ZentavioCRM.Core.DTOs.Audit;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string entityType, Guid entityId, string action, string summary, Guid? performedByUserId);

        Task<IReadOnlyList<AuditLogDto>> GetForEntityAsync(string entityType, Guid entityId);
    }
}
