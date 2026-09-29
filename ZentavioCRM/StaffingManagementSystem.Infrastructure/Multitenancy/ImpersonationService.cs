using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="IImpersonationService"/>
    public class ImpersonationService : IImpersonationService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly TenancySettings _settings;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IPlatformAuditLogService _auditLog;

        public ImpersonationService(
            PlatformDbContext platformDb,
            IOptions<TenancySettings> tenancyOptions,
            IJwtTokenGenerator jwtTokenGenerator,
            IPlatformAuditLogService auditLog)
        {
            _platformDb = platformDb;
            _settings = tenancyOptions.Value;
            _jwtTokenGenerator = jwtTokenGenerator;
            _auditLog = auditLog;
        }

        public async Task<ApiResponse<ImpersonateTenantResponseDto>> ImpersonateAsync(Guid tenantId, string? reason, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return ApiResponse<ImpersonateTenantResponseDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            // A Suspended/Terminated tenant's own traffic is already rejected by
            // TenantResolutionMiddleware regardless of what token it carries, so this check is
            // defense in depth, not the only thing stopping it — but failing fast here gives a
            // clear reason instead of minting a token that would 403 the moment it's used, and
            // means the audit trail never has to distinguish "impersonated a locked-out tenant."
            if (tenant.Status != TenantStatus.Active)
            {
                return ApiResponse<ImpersonateTenantResponseDto>.FailureResponse(
                    $"This tenant is {tenant.Status}, not Active — it cannot be impersonated.",
                    ["Reactivate the tenant first if you need to sign in as its admin."]);
            }

            var connectionString = $"{_settings.SqlServerHostConnectionString};Database={tenant.DatabaseName};";
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
            await using var tenantDb = new AppDbContext(options);

            // The tenant's admin user, identified the same way the platform admin list already
            // denormalizes it (Tenant.AdminEmail) — deliberately not "any user on this tenant":
            // impersonation is a support tool for reaching the account owner, not a way to become
            // an arbitrary employee of a customer's company.
            var user = await tenantDb.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == tenant.AdminEmail && u.IsActive);

            if (user is null)
            {
                return ApiResponse<ImpersonateTenantResponseDto>.FailureResponse(
                    "No active admin user found for this tenant.",
                    [$"Expected an active user with email \"{tenant.AdminEmail}\"."]);
            }

            var extraClaims = new Dictionary<string, string>
            {
                [IJwtTokenGenerator.ImpersonationClaimType] = "true",
                [IJwtTokenGenerator.ImpersonatedByClaimType] = performedByAdminId?.ToString() ?? string.Empty,
            };
            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user, extraClaims);

            var platformAdminEmail = performedByAdminId.HasValue
                ? (await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Id == performedByAdminId.Value))?.Email
                : null;
            var summary = string.IsNullOrWhiteSpace(reason)
                ? $"{platformAdminEmail ?? "A platform admin"} impersonated {user.Email} in tenant \"{tenant.Name}\"."
                : $"{platformAdminEmail ?? "A platform admin"} impersonated {user.Email} in tenant \"{tenant.Name}\". Reason: {reason.Trim()}";

            // Best-effort: written into the TENANT's own audit trail (not the Platform database)
            // so that tenant's own admin can see, from inside their own app, that platform support
            // signed in as them — transparency, not just a platform-side record. PerformedByUserId
            // stays null because the actor is a PlatformAdmin, a different account system entirely
            // with no row in this tenant's Users table; the summary text carries who did it instead.
            try
            {
                tenantDb.AuditLogs.Add(new AuditLog
                {
                    EntityType = "User",
                    EntityId = user.Id,
                    Action = "Impersonated",
                    Summary = summary,
                    PerformedByUserId = null,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                await tenantDb.SaveChangesAsync();
            }
            catch
            {
                // ignored — the PlatformAuditLog entry below is the source of truth for this action.
            }

            await _auditLog.LogAsync(performedByAdminId, "TenantImpersonated", summary, tenant.Id);

            return ApiResponse<ImpersonateTenantResponseDto>.SuccessResponse(new ImpersonateTenantResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                TenantSubdomain = tenant.Subdomain,
                ImpersonatedUserId = user.Id,
                ImpersonatedUserEmail = user.Email,
                ImpersonatedUserFullName = user.FullName,
            });
        }
    }
}
