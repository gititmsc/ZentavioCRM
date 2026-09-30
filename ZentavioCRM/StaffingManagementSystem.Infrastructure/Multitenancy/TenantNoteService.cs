using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="ITenantNoteService"/>
    public class TenantNoteService : ITenantNoteService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly IPlatformAuditLogService _auditLog;

        public TenantNoteService(PlatformDbContext platformDb, IPlatformAuditLogService auditLog)
        {
            _platformDb = platformDb;
            _auditLog = auditLog;
        }

        public async Task<ApiResponse<TenantNoteDto>> AddNoteAsync(Guid tenantId, CreateTenantNoteRequest request, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return ApiResponse<TenantNoteDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var note = new TenantNote
            {
                TenantId = tenant.Id,
                Note = request.Note.Trim(),
                CreatedByAdminId = performedByAdminId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _platformDb.TenantNotes.Add(note);
            await _platformDb.SaveChangesAsync();

            var admin = performedByAdminId.HasValue
                ? await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Id == performedByAdminId.Value)
                : null;

            await _auditLog.LogAsync(performedByAdminId, "TenantNoteAdded", $"Added a note to \"{tenant.Name}\".", tenant.Id);

            return ApiResponse<TenantNoteDto>.SuccessResponse(Map(note, admin?.Email), "Note added.");
        }

        public async Task<ApiResponse<IReadOnlyList<TenantNoteDto>>> GetNotesAsync(Guid tenantId)
        {
            if (!await _platformDb.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                return ApiResponse<IReadOnlyList<TenantNoteDto>>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var notes = await _platformDb.TenantNotes
                .Where(n => n.TenantId == tenantId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Select(n => new
                {
                    Note = n,
                    CreatedByAdminEmail = n.CreatedByAdmin != null ? n.CreatedByAdmin.Email : null,
                })
                .ToListAsync();

            var dtos = notes.Select(x => Map(x.Note, x.CreatedByAdminEmail)).ToList();
            return ApiResponse<IReadOnlyList<TenantNoteDto>>.SuccessResponse(dtos);
        }

        private static TenantNoteDto Map(TenantNote n, string? createdByAdminEmail) => new()
        {
            Id = n.Id,
            TenantId = n.TenantId,
            Note = n.Note,
            CreatedByAdminEmail = createdByAdminEmail ?? "System",
            CreatedAtUtc = n.CreatedAtUtc,
        };
    }
}
