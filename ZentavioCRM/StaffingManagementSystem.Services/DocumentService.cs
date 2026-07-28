using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Documents;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IDocumentService"/>
    public class DocumentService : IDocumentService
    {
        private const long MaxSizeBytes = 10 * 1024 * 1024;

        private readonly IDocumentRepository _documentRepository;
        private readonly IAuditLogService _auditLogService;

        public DocumentService(IDocumentRepository documentRepository, IAuditLogService auditLogService)
        {
            _documentRepository = documentRepository;
            _auditLogService = auditLogService;
        }

        public async Task<ApiResponse<DocumentDto>> UploadAsync(
            string entityType, Guid entityId, string fileName, string contentType, byte[] content, Guid? currentUserId)
        {
            if (content.Length == 0)
            {
                return ApiResponse<DocumentDto>.FailureResponse("The uploaded file is empty.");
            }

            if (content.Length > MaxSizeBytes)
            {
                return ApiResponse<DocumentDto>.FailureResponse("Files larger than 10 MB are not supported.");
            }

            var document = new Document
            {
                EntityType = entityType,
                EntityId = entityId,
                FileName = fileName,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                SizeBytes = content.Length,
                Content = content,
                UploadedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _documentRepository.AddAsync(document);
            await _auditLogService.LogAsync(entityType, entityId, "DocumentUploaded", $"Attached file \"{fileName}\".", currentUserId);

            return ApiResponse<DocumentDto>.SuccessResponse(Map(document), "File uploaded.");
        }

        public async Task<IReadOnlyList<DocumentDto>> GetForEntityAsync(string entityType, Guid entityId)
        {
            var documents = await _documentRepository.GetForEntityAsync(entityType, entityId);
            return documents.Select(Map).ToList();
        }

        public async Task<(string FileName, string ContentType, byte[] Content)?> DownloadAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            return document is null ? null : (document.FileName, document.ContentType, document.Content);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document is null)
            {
                return ApiResponse<bool>.FailureResponse("File not found.");
            }

            await _documentRepository.DeleteAsync(document);
            await _auditLogService.LogAsync(document.EntityType, document.EntityId, "DocumentDeleted", $"Removed file \"{document.FileName}\".", currentUserId);

            return ApiResponse<bool>.SuccessResponse(true, "File deleted.");
        }

        private static DocumentDto Map(Document document) => new()
        {
            Id = document.Id,
            EntityType = document.EntityType,
            EntityId = document.EntityId,
            FileName = document.FileName,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            UploadedByUserName = document.UploadedByUser?.FullName,
            CreatedAtUtc = document.CreatedAtUtc,
        };
    }
}
