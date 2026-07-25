using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Users;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    [Authorize]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // Intentionally just [Authorize] rather than gated on Users.View: the assignment
        // dropdowns on Leads/Customers need the user directory for every authenticated user,
        // not just those with the Users administration permission. Only Create/Update (actually
        // managing accounts) require Users.Manage.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<UserDto>>.SuccessResponse(users));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _userService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.UsersManage)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _userService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.UsersManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _userService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
