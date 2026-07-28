using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Delegations;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>
    /// Self-service out-of-office delegation setup. Gated only by [Authorize] — like Documents/
    /// AuditLogs, this isn't behind a module permission because every user manages their own
    /// delegations regardless of role.
    /// </summary>
    [ApiController]
    [Route("api/user-delegations")]
    [Produces("application/json")]
    [Authorize]
    public sealed class UserDelegationsController : ControllerBase
    {
        private readonly IUserDelegationService _delegationService;

        public UserDelegationsController(IUserDelegationService delegationService)
        {
            _delegationService = delegationService;
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var delegations = await _delegationService.GetForCurrentUserAsync(userId.Value);
            return Ok(ApiResponse<IReadOnlyList<UserDelegationDto>>.SuccessResponse(delegations));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveUserDelegationRequest request)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDelegationDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _delegationService.CreateAsync(userId.Value, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _delegationService.DeleteAsync(id, userId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
