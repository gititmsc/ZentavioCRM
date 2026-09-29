using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="ITenantUsageService"/>
    public class TenantUsageService : ITenantUsageService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly TenancySettings _settings;

        public TenantUsageService(PlatformDbContext platformDb, IOptions<TenancySettings> tenancyOptions)
        {
            _platformDb = platformDb;
            _settings = tenancyOptions.Value;
        }

        public async Task<ApiResponse<TenantUsageDto>> GetUsageAsync(Guid tenantId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return ApiResponse<TenantUsageDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            // A tenant that never finished provisioning (Provisioning/Failed) has no usable
            // database to connect to — everything is zero rather than an error, since "no usage
            // yet" is the accurate answer, not a failure.
            if (tenant.DatabaseName is not { Length: > 0 })
            {
                return ApiResponse<TenantUsageDto>.SuccessResponse(new TenantUsageDto
                {
                    TenantId = tenant.Id,
                    PlanTier = tenant.PlanTier,
                    MaxUsers = tenant.MaxUsers,
                    MaxRecords = tenant.MaxRecords,
                    MaxStorageMB = tenant.MaxStorageMB,
                });
            }

            var connectionString = $"{_settings.SqlServerHostConnectionString};Database={tenant.DatabaseName};";
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
            await using var tenantDb = new AppDbContext(options);

            var userCount = await tenantDb.Users.CountAsync(u => u.IsActive);
            var leadCount = await tenantDb.Leads.CountAsync();
            var customerCount = await tenantDb.Customers.CountAsync();
            var opportunityCount = await tenantDb.Opportunities.CountAsync();
            var quotationCount = await tenantDb.Quotations.CountAsync();
            var salesOrderCount = await tenantDb.SalesOrders.CountAsync();

            var lastActivityAtUtc = await tenantDb.Users
                .Where(u => u.LastLoginAtUtc != null)
                .OrderByDescending(u => u.LastLoginAtUtc)
                .Select(u => u.LastLoginAtUtc)
                .FirstOrDefaultAsync();

            // Data file size only (type = 0) — log file growth is maintenance overhead, not
            // customer data volume, so it's excluded from what counts against MaxStorageMB.
            var databaseSizeMB = await tenantDb.Database
                .SqlQueryRaw<decimal>(
                    "SELECT CAST(ISNULL(SUM(size), 0) * 8.0 / 1024 AS DECIMAL(18,2)) AS Value FROM sys.database_files WHERE type = 0")
                .FirstOrDefaultAsync();

            return ApiResponse<TenantUsageDto>.SuccessResponse(new TenantUsageDto
            {
                TenantId = tenant.Id,
                PlanTier = tenant.PlanTier,
                UserCount = userCount,
                MaxUsers = tenant.MaxUsers,
                RecordCount = leadCount + customerCount + opportunityCount + quotationCount + salesOrderCount,
                MaxRecords = tenant.MaxRecords,
                DatabaseSizeMB = databaseSizeMB,
                MaxStorageMB = tenant.MaxStorageMB,
                LastActivityAtUtc = lastActivityAtUtc,
            });
        }
    }
}
