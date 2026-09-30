using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Api.Middleware
{
    /// <summary>
    /// Figures out which tenant a request belongs to — from the X-Tenant header first (what the
    /// frontend sends, and the only practical option for local development), falling back to the
    /// subdomain of the Host header for real subdomain-based routing in production — then looks
    /// it up in the Platform database and populates <see cref="ITenantContext"/> so AppDbContext
    /// connects to that tenant's own database for the rest of the request.
    ///
    /// Runs before authentication: which database to query is a prerequisite for authenticating
    /// a user at all (each tenant has its own Users table).
    /// </summary>
    public class TenantResolutionMiddleware
    {
        /// <summary>Path prefixes that operate outside any single tenant and must bypass resolution entirely.</summary>
        private static readonly string[] BypassPrefixes = ["/api/platform", "/swagger", "/favicon.ico"];

        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            PlatformDbContext platformDbContext,
            IOptions<TenancySettings> tenancyOptions,
            IPlatformAuditLogService auditLog)
        {
            if (BypassPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
            {
                await _next(context);
                return;
            }

            var settings = tenancyOptions.Value;
            var subdomain = ResolveSubdomain(context, settings);

            if (subdomain is not null)
            {
                var tenant = await platformDbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Subdomain == subdomain);

                if (tenant is null)
                {
                    await WriteErrorAsync(context, StatusCodes.Status404NotFound, $"No tenant found for \"{subdomain}\".");
                    return;
                }

                // Reactive trial expiration — mirrors the existing Overdue-auto-suspend pattern
                // (TenantBillingService.UpdateBillingAsync): there's no background job
                // infrastructure anywhere in this app, so an expired-but-still-Active Trial tenant
                // is only caught the next time one of its own requests passes through here, not on
                // a schedule. tenant.PlanTier still being Trial guards against acting on a tenant
                // that was upgraded to a paid tier without ever clearing its old TrialEndsAtUtc.
                if (tenant.Status == TenantStatus.Active
                    && tenant.PlanTier == PlanTier.Trial
                    && tenant.TrialEndsAtUtc is { } trialEndsAtUtc
                    && trialEndsAtUtc <= DateTime.UtcNow)
                {
                    await platformDbContext.Tenants
                        .Where(t => t.Id == tenant.Id)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Status, TenantStatus.Suspended));

                    await auditLog.LogAsync(
                        null,
                        "TenantTrialExpired",
                        $"Tenant \"{tenant.Name}\" was automatically suspended — its {PlanTierDefaults.TrialLengthDays}-day trial expired.",
                        tenant.Id);

                    await WriteErrorAsync(context, StatusCodes.Status403Forbidden, $"Tenant \"{subdomain}\" is Suspended and cannot be accessed.");
                    return;
                }

                if (tenant.Status != TenantStatus.Active)
                {
                    await WriteErrorAsync(context, StatusCodes.Status403Forbidden, $"Tenant \"{subdomain}\" is {tenant.Status} and cannot be accessed.");
                    return;
                }

                tenantContext.Resolve(tenant.Id, subdomain, BuildConnectionString(settings, tenant));
            }

            // No subdomain resolved at all (e.g. bare http://localhost in local dev) — left
            // unresolved. AppDbContext's factory falls back to Tenancy:DefaultTenantConnectionStringName
            // if one is configured, or throws a clear error otherwise.
            await _next(context);
        }

        private static string? ResolveSubdomain(HttpContext context, TenancySettings settings)
        {
            if (context.Request.Headers.TryGetValue(settings.TenantHeaderName, out var headerValue))
            {
                var fromHeader = headerValue.ToString().Trim().ToLowerInvariant();
                if (fromHeader.Length > 0)
                {
                    return fromHeader;
                }
            }

            var host = context.Request.Host.Host.ToLowerInvariant();

            if (!string.IsNullOrEmpty(settings.RootDomain) && host.EndsWith("." + settings.RootDomain, StringComparison.Ordinal))
            {
                var label = host[..^(settings.RootDomain.Length + 1)];
                return label is "" or "www" ? null : label;
            }

            return null;
        }

        private static string BuildConnectionString(TenancySettings settings, Tenant tenant)
            => $"{settings.SqlServerHostConnectionString};Database={tenant.DatabaseName};";

        private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { success = false, message, errors = new[] { message } });
        }
    }
}
