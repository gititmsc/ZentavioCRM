using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Documents;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<ApiResponse<DocumentDto>> UploadAsync(
            string entityType, Guid entityId, string fileName, string contentType, byte[] content, Guid? currentUserId);

        Task<IReadOnlyList<DocumentDto>> GetForEntityAsync(string entityType, Guid entityId);

        /// <summary>Returns the raw file for download, or null if not found.</summary>
        Task<(string FileName, string ContentType, byte[] Content)?> DownloadAsync(Guid id);

        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId);
    }
}
