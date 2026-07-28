using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Documents;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>
    /// Generic file attachments for any CRM record (Customer, Opportunity, ...). Gated only by
    /// [Authorize] — like AuditLogs/Notifications, attachments cut across module permissions
    /// rather than belonging to one, so this milestone doesn't introduce a dedicated
    /// Documents.* permission set.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    [Produces("application/json")]
    [Authorize]
    public sealed class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetForEntity([FromQuery] string entityType, [FromQuery] Guid entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                return BadRequest(ApiResponse<List<DocumentDto>>.FailureResponse("entityType is required."));
            }

            var documents = await _documentService.GetForEntityAsync(entityType, entityId);
            return Ok(ApiResponse<IReadOnlyList<DocumentDto>>.SuccessResponse(documents));
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload([FromForm] string entityType, [FromForm] Guid entityId, IFormFile file)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(ApiResponse<DocumentDto>.FailureResponse("No file was uploaded."));
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            var result = await _documentService.UploadAsync(entityType, entityId, file.FileName, file.ContentType, stream.ToArray(), User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var file = await _documentService.DownloadAsync(id);
            if (file is null)
            {
                return NotFound(ApiResponse<bool>.FailureResponse("File not found."));
            }

            return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _documentService.DeleteAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
