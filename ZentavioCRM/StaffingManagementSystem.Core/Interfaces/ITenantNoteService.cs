using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Free-text notes platform admins leave for each other on a tenant (see
    /// <see cref="Entities.Platform.TenantNote"/>) — append-only, no edit/delete for v1.
    /// </summary>
    public interface ITenantNoteService
    {
        Task<ApiResponse<TenantNoteDto>> AddNoteAsync(Guid tenantId, CreateTenantNoteRequest request, Guid? performedByAdminId);

        Task<ApiResponse<IReadOnlyList<TenantNoteDto>>> GetNotesAsync(Guid tenantId);
    }
}
