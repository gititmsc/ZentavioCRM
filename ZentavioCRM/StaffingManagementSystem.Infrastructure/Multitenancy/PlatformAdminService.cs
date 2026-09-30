using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="IPlatformAdminService"/>
    public class PlatformAdminService : IPlatformAdminService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPlatformJwtTokenGenerator _jwtTokenGenerator;
        private readonly IPlatformAuditLogService _auditLog;

        /// <summary>Consecutive bad-password attempts before an account is temporarily locked.</summary>
        private const int MaxFailedLoginAttempts = 5;

        /// <summary>How long a lockout lasts once <see cref="MaxFailedLoginAttempts"/> is reached.</summary>
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public PlatformAdminService(
            PlatformDbContext platformDb,
            IPasswordHasher passwordHasher,
            IPlatformJwtTokenGenerator jwtTokenGenerator,
            IPlatformAuditLogService auditLog)
        {
            _platformDb = platformDb;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _auditLog = auditLog;
        }

        public async Task<ApiResponse<PlatformLoginResponseDto>> LoginAsync(PlatformLoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var admin = await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Email == email);

            // Same generic message for "no such account", "inactive", "locked" and "wrong password"
            // — an attacker probing emails shouldn't be able to tell which case they hit. The one
            // exception is a lockout already in effect: that's surfaced distinctly below so a
            // legitimate admin knows to wait rather than keep retrying (which would only push the
            // lockout further out once it expires and the counter resumes).
            if (admin is not null && admin.LockedUntilUtc is { } lockedUntil && lockedUntil > DateTime.UtcNow)
            {
                return ApiResponse<PlatformLoginResponseDto>.FailureResponse(
                    "This account is temporarily locked due to repeated failed login attempts. Please try again later.",
                    ["Account locked."]);
            }

            if (admin is null || !admin.IsActive || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
            {
                if (admin is not null && admin.IsActive)
                {
                    // A lockout that just expired resets silently on the next attempt — the failed
                    // counter below then starts counting fresh from that attempt.
                    if (admin.LockedUntilUtc is not null && admin.LockedUntilUtc <= DateTime.UtcNow)
                    {
                        admin.FailedLoginAttempts = 0;
                        admin.LockedUntilUtc = null;
                    }

                    admin.FailedLoginAttempts++;
                    if (admin.FailedLoginAttempts >= MaxFailedLoginAttempts)
                    {
                        admin.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                        await _platformDb.SaveChangesAsync();
                        await _auditLog.LogAsync(admin.Id, "AccountLocked",
                            $"{admin.Email} was locked out after {admin.FailedLoginAttempts} failed login attempts.");
                    }
                    else
                    {
                        await _platformDb.SaveChangesAsync();
                    }
                }

                return ApiResponse<PlatformLoginResponseDto>.FailureResponse(
                    "Invalid email or password.",
                    ["Invalid email or password."]);
            }

            admin.FailedLoginAttempts = 0;
            admin.LockedUntilUtc = null;
            admin.LastLoginAtUtc = DateTime.UtcNow;
            await _platformDb.SaveChangesAsync();

            await _auditLog.LogAsync(admin.Id, "Login", $"{admin.Email} logged in.");

            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(admin);

            return ApiResponse<PlatformLoginResponseDto>.SuccessResponse(
                new PlatformLoginResponseDto
                {
                    Token = token,
                    ExpiresAtUtc = expiresAtUtc,
                    Admin = Map(admin),
                },
                "Login successful.");
        }

        public async Task<IReadOnlyList<PlatformAdminDto>> GetAllAsync()
            => await _platformDb.PlatformAdmins
                .OrderBy(a => a.Email)
                .Select(a => new PlatformAdminDto
                {
                    Id = a.Id,
                    Email = a.Email,
                    FullName = (a.FirstName + " " + a.LastName).Trim(),
                    IsActive = a.IsActive,
                    Role = a.Role,
                    CreatedAtUtc = a.CreatedAtUtc,
                    LastLoginAtUtc = a.LastLoginAtUtc,
                    IsLockedOut = a.LockedUntilUtc != null && a.LockedUntilUtc > DateTime.UtcNow,
                })
                .ToListAsync();

        public async Task<ApiResponse<PlatformAdminDto>> CreateAsync(CreatePlatformAdminRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _platformDb.PlatformAdmins.AnyAsync(a => a.Email == email))
            {
                return ApiResponse<PlatformAdminDto>.FailureResponse(
                    "A platform admin with this email already exists.",
                    ["Choose a different email address."]);
            }

            var admin = new PlatformAdmin
            {
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                IsActive = true,
                Role = request.Role,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _platformDb.PlatformAdmins.Add(admin);
            await _platformDb.SaveChangesAsync();

            return ApiResponse<PlatformAdminDto>.SuccessResponse(Map(admin), "Platform admin created.");
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid adminId, ChangePlatformAdminPasswordRequest request)
        {
            var admin = await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Id == adminId);
            if (admin is null)
            {
                return ApiResponse<bool>.FailureResponse("Admin not found.", ["Admin not found."]);
            }

            if (!_passwordHasher.Verify(request.CurrentPassword, admin.PasswordHash))
            {
                return ApiResponse<bool>.FailureResponse(
                    "Current password is incorrect.",
                    ["Current password is incorrect."]);
            }

            admin.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            await _platformDb.SaveChangesAsync();

            await _auditLog.LogAsync(admin.Id, "PasswordChanged", $"{admin.Email} changed their password.");

            return ApiResponse<bool>.SuccessResponse(true, "Password changed.");
        }

        private static PlatformAdminDto Map(PlatformAdmin admin) => new()
        {
            Id = admin.Id,
            Email = admin.Email,
            FullName = admin.FullName,
            IsActive = admin.IsActive,
            Role = admin.Role,
            CreatedAtUtc = admin.CreatedAtUtc,
            LastLoginAtUtc = admin.LastLoginAtUtc,
            IsLockedOut = admin.LockedUntilUtc is { } lockedUntil && lockedUntil > DateTime.UtcNow,
        };
    }
}
