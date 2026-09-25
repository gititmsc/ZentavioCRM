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

        public PlatformAdminService(
            PlatformDbContext platformDb,
            IPasswordHasher passwordHasher,
            IPlatformJwtTokenGenerator jwtTokenGenerator)
        {
            _platformDb = platformDb;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<ApiResponse<PlatformLoginResponseDto>> LoginAsync(PlatformLoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var admin = await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Email == email);

            if (admin is null || !admin.IsActive || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
            {
                return ApiResponse<PlatformLoginResponseDto>.FailureResponse(
                    "Invalid email or password.",
                    ["Invalid email or password."]);
            }

            admin.LastLoginAtUtc = DateTime.UtcNow;
            await _platformDb.SaveChangesAsync();

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
                    CreatedAtUtc = a.CreatedAtUtc,
                    LastLoginAtUtc = a.LastLoginAtUtc,
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
                CreatedAtUtc = DateTime.UtcNow,
            };

            _platformDb.PlatformAdmins.Add(admin);
            await _platformDb.SaveChangesAsync();

            return ApiResponse<PlatformAdminDto>.SuccessResponse(Map(admin), "Platform admin created.");
        }

        private static PlatformAdminDto Map(PlatformAdmin admin) => new()
        {
            Id = admin.Id,
            Email = admin.Email,
            FullName = admin.FullName,
            IsActive = admin.IsActive,
            CreatedAtUtc = admin.CreatedAtUtc,
            LastLoginAtUtc = admin.LastLoginAtUtc,
        };
    }
}
