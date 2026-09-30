using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="ITenantAdminActionsService"/>
    public class TenantAdminActionsService : ITenantAdminActionsService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly TenancySettings _tenancySettings;
        private readonly JwtSettings _jwtSettings;
        private readonly FrontendSettings _frontendSettings;
        private readonly ISecureTokenGenerator _secureTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IPlatformAuditLogService _auditLog;
        private readonly ILogger<TenantAdminActionsService> _logger;

        public TenantAdminActionsService(
            PlatformDbContext platformDb,
            IOptions<TenancySettings> tenancyOptions,
            IOptions<JwtSettings> jwtOptions,
            IOptions<FrontendSettings> frontendOptions,
            ISecureTokenGenerator secureTokenGenerator,
            IEmailService emailService,
            IPlatformAuditLogService auditLog,
            ILogger<TenantAdminActionsService> logger)
        {
            _platformDb = platformDb;
            _tenancySettings = tenancyOptions.Value;
            _jwtSettings = jwtOptions.Value;
            _frontendSettings = frontendOptions.Value;
            _secureTokenGenerator = secureTokenGenerator;
            _emailService = emailService;
            _auditLog = auditLog;
            _logger = logger;
        }

        public Task<ApiResponse<bool>> ResendWelcomeEmailAsync(Guid tenantId, Guid? performedByAdminId)
            => SendResetLinkAsync(
                tenantId,
                performedByAdminId,
                actionName: "ResendWelcomeEmail",
                summaryVerb: "resent the welcome email to",
                buildSubjectAndBody: (user, resetLink, expiryMinutes) => (
                    Subject: "Welcome to ZentavioCRM",
                    HtmlBody:
                        $"<p>Hello {user.FullName},</p>" +
                        "<p>You've been invited to ZentavioCRM. Use the link below to set your password and sign in " +
                        $"— it expires in {expiryMinutes} minutes and can only be used once:</p>" +
                        $"<p><a href=\"{resetLink}\">{resetLink}</a></p>" +
                        "<p>If you weren't expecting this, you can safely ignore this email.</p>"));

        public Task<ApiResponse<bool>> ForcePasswordResetAsync(Guid tenantId, Guid? performedByAdminId)
            => SendResetLinkAsync(
                tenantId,
                performedByAdminId,
                actionName: "ForcePasswordReset",
                summaryVerb: "triggered a password reset for",
                buildSubjectAndBody: (user, resetLink, expiryMinutes) => (
                    Subject: "Reset your ZentavioCRM password",
                    HtmlBody:
                        $"<p>Hello {user.FullName},</p>" +
                        "<p>A ZentavioCRM administrator has requested a password reset for your account. Use the link " +
                        $"below to choose a new password — it expires in {expiryMinutes} minutes and can only be used once:</p>" +
                        $"<p><a href=\"{resetLink}\">{resetLink}</a></p>" +
                        "<p>If you weren't expecting this, contact your administrator — your password has not been changed yet.</p>"));

        /// <summary>Shared mechanics for both quick actions: both just mail the tenant's admin a
        /// fresh password-reset link, differing only in subject/copy and which audit action name is
        /// recorded. Mirrors AuthService.ForgotPasswordAsync's token-generation logic, and
        /// ImpersonationService's ad-hoc cross-tenant-database connection pattern — this is a
        /// Platform-side action targeting one specific tenant's own database, not the current
        /// request's own (TenantResolutionMiddleware never resolves a tenant for /api/platform
        /// routes, so there is no "current" tenant database to reuse here).</summary>
        private async Task<ApiResponse<bool>> SendResetLinkAsync(
            Guid tenantId,
            Guid? performedByAdminId,
            string actionName,
            string summaryVerb,
            Func<User, string, int, (string Subject, string HtmlBody)> buildSubjectAndBody)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return ApiResponse<bool>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            // Same reasoning as ImpersonationService: a Suspended/Terminated tenant's users
            // already can't sign in regardless of what link we email them, so acting on a
            // non-Active tenant here would just be confusing, not useful.
            if (tenant.Status != TenantStatus.Active)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"This tenant is {tenant.Status}, not Active — this action isn't available.",
                    ["Reactivate the tenant first."]);
            }

            var connectionString = $"{_tenancySettings.SqlServerHostConnectionString};Database={tenant.DatabaseName};";
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
            await using var tenantDb = new AppDbContext(options);

            // Same denormalized Tenant.AdminEmail lookup ImpersonationService uses — the account
            // owner, not an arbitrary user on the tenant.
            var user = await tenantDb.Users.FirstOrDefaultAsync(u => u.Email == tenant.AdminEmail && u.IsActive);
            if (user is null)
            {
                return ApiResponse<bool>.FailureResponse(
                    "No active admin user found for this tenant.",
                    [$"Expected an active user with email \"{tenant.AdminEmail}\"."]);
            }

            var expiryMinutes = _jwtSettings.PasswordResetTokenExpiryMinutes;

            try
            {
                var rawToken = _secureTokenGenerator.GenerateRawToken();

                tenantDb.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = _secureTokenGenerator.Hash(rawToken),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes),
                    CreatedAtUtc = DateTime.UtcNow,
                });
                await tenantDb.SaveChangesAsync();

                var resetLink = $"{_frontendSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
                var (subject, htmlBody) = buildSubjectAndBody(user, resetLink, expiryMinutes);

                await _emailService.SendAsync(user.Email, subject, htmlBody);
            }
            catch (Exception ex)
            {
                // Unlike the tenant's own self-service ForgotPasswordAsync, there's no enumeration
                // concern to protect here — the caller is an authenticated platform admin who
                // already knows this tenant and its admin email, so a failure is surfaced directly
                // rather than swallowed behind a generic success message.
                _logger.LogError(ex, "Failed to send {Action} email for tenant {TenantId}.", actionName, tenantId);
                return ApiResponse<bool>.FailureResponse(
                    "Could not send the email. Please try again.",
                    ["The email could not be sent — check the SMTP configuration and try again."]);
            }

            var platformAdminEmail = performedByAdminId.HasValue
                ? (await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Id == performedByAdminId.Value))?.Email
                : null;
            var summary = $"{platformAdminEmail ?? "A platform admin"} {summaryVerb} {user.Email} in tenant \"{tenant.Name}\".";

            // Dual audit logging, same as ImpersonationService: best-effort into the tenant's own
            // AuditLogs (so that tenant's own admin can see platform support acted on their
            // account) plus the authoritative PlatformAuditLogs entry.
            try
            {
                tenantDb.AuditLogs.Add(new AuditLog
                {
                    EntityType = "User",
                    EntityId = user.Id,
                    Action = actionName,
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

            await _auditLog.LogAsync(performedByAdminId, actionName, summary, tenant.Id);

            return ApiResponse<bool>.SuccessResponse(true, "Email sent.");
        }
    }
}
