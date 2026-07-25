using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Auth;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IAuthService"/>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

            if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "Invalid email or password.",
                    ["Invalid email or password."]);
            }

            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
            await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

            var permissions = user.Role?.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.Code)
                .ToList() ?? [];

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role?.Name ?? string.Empty,
                    Permissions = permissions,
                },
            };

            return ApiResponse<LoginResponseDto>.SuccessResponse(response, "Login successful.");
        }
    }
}
