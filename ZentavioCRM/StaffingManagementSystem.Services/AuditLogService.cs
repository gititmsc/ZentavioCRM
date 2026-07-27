using ZentavioCRM.Core.DTOs.Audit;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IAuditLogService"/>
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public Task LogAsync(string entityType, Guid entityId, string action, string summary, Guid? performedByUserId)
            => _auditLogRepository.AddAsync(new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Summary = summary,
                PerformedByUserId = performedByUserId,
                CreatedAtUtc = DateTime.UtcNow,
            });

        public async Task<IReadOnlyList<AuditLogDto>> GetForEntityAsync(string entityType, Guid entityId)
        {
            var logs = await _auditLogRepository.GetForEntityAsync(entityType, entityId);

            return logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Action = l.Action,
                Summary = l.Summary,
                PerformedByUserName = l.PerformedByUser?.FullName,
                CreatedAtUtc = l.CreatedAtUtc,
            }).ToList();
        }
    }
}
