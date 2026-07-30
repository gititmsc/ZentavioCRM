using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.DTOs.Auth;
using ZentavioCRM.Core.Entities;
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
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ISecureTokenGenerator _secureTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly JwtSettings _jwtSettings;
        private readonly FrontendSettings _frontendSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            ISecureTokenGenerator secureTokenGenerator,
            IEmailService emailService,
            IOptions<JwtSettings> jwtSettings,
            IOptions<FrontendSettings> frontendSettings,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _secureTokenGenerator = secureTokenGenerator;
            _emailService = emailService;
            _jwtSettings = jwtSettings.Value;
            _frontendSettings = frontendSettings.Value;
            _logger = logger;
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

            await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

            var response = await BuildLoginResponseAsync(user);

            return ApiResponse<LoginResponseDto>.SuccessResponse(response, "Login successful.");
        }

        public async Task<ApiResponse<LoginResponseDto>> RefreshAsync(string refreshToken)
        {
            const string genericError = "Your session has expired. Please log in again.";

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(genericError, [genericError]);
            }

            var tokenHash = _secureTokenGenerator.Hash(refreshToken);
            var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (existingToken is null || existingToken.RevokedAtUtc is not null || existingToken.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(genericError, [genericError]);
            }

            // Claim the token for rotation atomically — a single conditional UPDATE, done BEFORE
            // minting anything — so if this same token is presented twice concurrently (a replay/
            // theft attempt, or just a network retry), only one caller can ever flip RevokedAtUtc
            // and only one can ever walk away with a new session. The other gets rejected exactly
            // like any other dead token, rather than both silently succeeding.
            var claimed = await _refreshTokenRepository.TryClaimForRotationAsync(existingToken.Id);
            if (!claimed)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(genericError, [genericError]);
            }

            var user = await _userRepository.GetByIdAsync(existingToken.UserId);

            if (user is null || !user.IsActive)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(genericError, [genericError]);
            }

            var response = await BuildLoginResponseAsync(user);
            await _refreshTokenRepository.SetReplacedByTokenHashAsync(existingToken.Id, _secureTokenGenerator.Hash(response.RefreshToken));

            return ApiResponse<LoginResponseDto>.SuccessResponse(response, "Session refreshed.");
        }

        public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var tokenHash = _secureTokenGenerator.Hash(refreshToken);
                var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

                if (existingToken is not null && existingToken.RevokedAtUtc is null)
                {
                    existingToken.RevokedAtUtc = DateTime.UtcNow;
                    await _refreshTokenRepository.UpdateAsync(existingToken);
                }
            }

            // Always succeeds — logging out with an already-invalid/missing token is still a
            // successful logout from the client's point of view (the session ends either way).
            return ApiResponse<bool>.SuccessResponse(true, "Logged out.");
        }

        public async Task<ApiResponse<bool>> ForgotPasswordAsync(string email)
        {
            const string genericMessage =
                "If an account exists for that email address, a password reset link has been sent.";

            var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());

            // Deliberately identical response whether or not the account exists/is active — this
            // endpoint must never let a caller learn which email addresses have accounts. That
            // includes exception behavior: an SMTP failure (bad credentials, host unreachable,
            // timeout) must not surface as a 500 only on the "account exists" branch, or the
            // difference between "200 always" and "sometimes 500" itself becomes an enumeration
            // oracle. So this entire branch is swallowed — logged, never rethrown.
            if (user is not null && user.IsActive)
            {
                try
                {
                    var rawToken = _secureTokenGenerator.GenerateRawToken();

                    await _passwordResetTokenRepository.AddAsync(new PasswordResetToken
                    {
                        UserId = user.Id,
                        TokenHash = _secureTokenGenerator.Hash(rawToken),
                        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.PasswordResetTokenExpiryMinutes),
                        CreatedAtUtc = DateTime.UtcNow,
                    });

                    var resetLink =
                        $"{_frontendSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
                    var expiryMinutes = _jwtSettings.PasswordResetTokenExpiryMinutes;

                    var htmlBody =
                        $"<p>Hello {user.FullName},</p>" +
                        $"<p>We received a request to reset your ZentavioCRM password. This link expires in {expiryMinutes} minutes and can only be used once:</p>" +
                        $"<p><a href=\"{resetLink}\">{resetLink}</a></p>" +
                        "<p>If you didn't request this, you can safely ignore this email — your password will not be changed.</p>";

                    await _emailService.SendAsync(user.Email, "Reset your ZentavioCRM password", htmlBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email for user {UserId}.", user.Id);
                }
            }

            return ApiResponse<bool>.SuccessResponse(true, genericMessage);
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ApiResponse<bool>.FailureResponse(
                    "This password reset link is invalid.",
                    ["This password reset link is invalid."]);
            }

            var tokenHash = _secureTokenGenerator.Hash(token);
            var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash);

            if (resetToken is null)
            {
                return ApiResponse<bool>.FailureResponse(
                    "This password reset link is invalid.",
                    ["This password reset link is invalid."]);
            }

            if (resetToken.UsedAtUtc is not null)
            {
                return ApiResponse<bool>.FailureResponse(
                    "This password reset link has already been used. Please request a new one.",
                    ["This password reset link has already been used. Please request a new one."]);
            }

            if (resetToken.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return ApiResponse<bool>.FailureResponse(
                    "This password reset link has expired. Please request a new one.",
                    ["This password reset link has expired. Please request a new one."]);
            }

            // Claim the token atomically — a single conditional UPDATE — before touching the
            // password, so two concurrent submissions of the same emailed link can't both succeed.
            // Losing this race is indistinguishable from "already used" from the caller's side.
            var claimed = await _passwordResetTokenRepository.TryConsumeAsync(resetToken.Id);
            if (!claimed)
            {
                return ApiResponse<bool>.FailureResponse(
                    "This password reset link has already been used. Please request a new one.",
                    ["This password reset link has already been used. Please request a new one."]);
            }

            var user = await _userRepository.GetByIdAsync(resetToken.UserId);

            if (user is null || !user.IsActive)
            {
                return ApiResponse<bool>.FailureResponse(
                    "This password reset link is invalid.",
                    ["This password reset link is invalid."]);
            }

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            await _userRepository.UpdateAsync(user);

            // Someone other than the currently-signed-in user (if any) may have acted here — end
            // every existing session on this account, the same way an admin-triggered reset would.
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);

            return ApiResponse<bool>.SuccessResponse(true, "Your password has been reset. Please log in.");
        }

        public async Task<ApiResponse<LoginResponseDto>> IssueSessionAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null || !user.IsActive)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse("User not found.", ["User not found."]);
            }

            var response = await BuildLoginResponseAsync(user);
            return ApiResponse<LoginResponseDto>.SuccessResponse(response);
        }

        private async Task<LoginResponseDto> BuildLoginResponseAsync(User user)
        {
            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);

            var rawRefreshToken = _secureTokenGenerator.GenerateRawToken();
            var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _secureTokenGenerator.Hash(rawRefreshToken),
                ExpiresAtUtc = refreshTokenExpiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
            });

            var permissions = user.Role?.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.Code)
                .ToList() ?? [];

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                RefreshToken = rawRefreshToken,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role?.Name ?? string.Empty,
                    Permissions = permissions,
                },
            };
        }
    }
}
