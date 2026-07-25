using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Activities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>
    /// Generic activity timeline (calls, emails, meetings, tasks, notes...) shared by every CRM record —
    /// reused by the Lead and Customer detail screens instead of each module inventing its own log.
    /// </summary>
    [ApiController]
    [Route("api/activities")]
    [Produces("application/json")]
    [Authorize]
    public sealed class ActivitiesController : ControllerBase
    {
        private readonly IActivityService _activityService;

        public ActivitiesController(IActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeline([FromQuery] RelatedEntityType relatedToType, [FromQuery] Guid relatedToId)
        {
            var timeline = await _activityService.GetTimelineAsync(relatedToType, relatedToId);
            return Ok(ApiResponse<IReadOnlyList<ActivityDto>>.SuccessResponse(timeline));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromQuery] RelatedEntityType relatedToType,
            [FromQuery] Guid relatedToId,
            [FromBody] CreateActivityRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ActivityDto>.FailureResponse("Validation failed.", errors));
            }

            var activity = await _activityService.CreateAsync(relatedToType, relatedToId, request, User.GetUserId());
            return Ok(ApiResponse<ActivityDto>.SuccessResponse(activity, "Activity logged."));
        }
    }
}
