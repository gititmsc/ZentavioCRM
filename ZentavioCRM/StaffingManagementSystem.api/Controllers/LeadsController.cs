using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/leads")]
    [Produces("application/json")]
    [Authorize]
    public sealed class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly ILeadScoringSettingsService _leadScoringSettingsService;
        private readonly IMergeService _mergeService;

        public LeadsController(ILeadService leadService, ILeadScoringSettingsService leadScoringSettingsService, IMergeService mergeService)
        {
            _leadService = leadService;
            _leadScoringSettingsService = leadScoringSettingsService;
            _mergeService = mergeService;
        }

        /// <summary>Current tenant-configurable lead-scoring weights — powers the Lead Scoring Settings screen.</summary>
        [HttpGet("scoring-settings")]
        [Authorize(Policy = PermissionCodes.LeadsManageScoring)]
        public async Task<IActionResult> GetScoringSettings()
        {
            var settings = await _leadScoringSettingsService.GetAsync();
            return Ok(ApiResponse<LeadScoringSettingsDto>.SuccessResponse(settings));
        }

        [HttpPut("scoring-settings")]
        [Authorize(Policy = PermissionCodes.LeadsManageScoring)]
        public async Task<IActionResult> UpdateScoringSettings([FromBody] LeadScoringSettingsDto request)
        {
            var result = await _leadScoringSettingsService.UpdateAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] LeadStatus? status,
            [FromQuery] Guid? assignedToUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            var result = await _leadService.SearchAsync(search, status, assignedToUserId, page, pageSize, User.GetUserId(), sortBy, sortDescending);
            return Ok(ApiResponse<PagedResult<LeadListItemDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _leadService.GetByIdAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.LeadsCreate)]
        public async Task<IActionResult> Create([FromBody] SaveLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.CreateAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.LeadsEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.UpdateAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("check-duplicates")]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> CheckDuplicates([FromQuery] string? email, [FromQuery] string? mobile, [FromQuery] Guid? excludeLeadId)
        {
            var result = await _leadService.CheckDuplicatesAsync(email, mobile, excludeLeadId);
            return Ok(ApiResponse<DuplicateCheckResultDto>.SuccessResponse(result));
        }

        [HttpGet("customer-lookup")]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> CustomerLookup([FromQuery] string? search)
        {
            var result = await _leadService.LookupCustomersByNameAsync(search, User.GetUserId());
            return Ok(ApiResponse<IReadOnlyList<CustomerLookupDto>>.SuccessResponse(result));
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = PermissionCodes.LeadsEdit)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateLeadStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.UpdateStatusAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Policy = PermissionCodes.LeadsAssign)]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.AssignAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/convert")]
        [Authorize(Policy = PermissionCodes.LeadsConvert)]
        public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertLeadRequest request)
        {
            var result = await _leadService.ConvertAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/convert-to-opportunity")]
        [Authorize(Policy = PermissionCodes.LeadsConvert)]
        public async Task<IActionResult> ConvertToOpportunity(Guid id, [FromBody] ConvertLeadToOpportunityRequest request)
        {
            var result = await _leadService.ConvertToOpportunityAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.LeadsDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _leadService.DeleteAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("export")]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> Export()
        {
            var csv = await _leadService.ExportCsvAsync();
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "leads.csv");
        }

        [HttpPost("import")]
        [Authorize(Policy = PermissionCodes.LeadsCreate)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file.Length == 0)
            {
                return BadRequest(ApiResponse<ImportResultDto>.FailureResponse("No file was uploaded."));
            }

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var result = await _leadService.ImportCsvAsync(content, User.GetUserId());
            return Ok(ApiResponse<ImportResultDto>.SuccessResponse(result, $"Imported {result.SuccessCount} of {result.TotalRows} rows."));
        }

        [HttpGet("export-xlsx")]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> ExportXlsx()
        {
            var bytes = await _leadService.ExportXlsxAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "leads.xlsx");
        }

        [HttpPost("import-xlsx")]
        [Authorize(Policy = PermissionCodes.LeadsCreate)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ImportXlsx(IFormFile file)
        {
            if (file.Length == 0)
            {
                return BadRequest(ApiResponse<ImportResultDto>.FailureResponse("No file was uploaded."));
            }

            using var stream = file.OpenReadStream();
            var result = await _leadService.ImportXlsxAsync(stream, User.GetUserId());
            return Ok(ApiResponse<ImportResultDto>.SuccessResponse(result, $"Imported {result.SuccessCount} of {result.TotalRows} rows."));
        }

        [HttpPost("merge")]
        [Authorize(Policy = PermissionCodes.LeadsDelete)]
        public async Task<IActionResult> Merge([FromBody] MergeLeadsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<MergeResultDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _mergeService.MergeLeadsAsync(request.SurvivingLeadId, request.LosingLeadId, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
