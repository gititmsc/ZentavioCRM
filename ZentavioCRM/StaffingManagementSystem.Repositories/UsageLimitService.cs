using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.DTOs.Usage;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IUsageLimitService"/>
    public class UsageLimitService : IUsageLimitService
    {
        private readonly AppDbContext _dbContext;
        private readonly ITenantContext _tenantContext;
        private readonly IPlanLimitService _planLimitService;

        public UsageLimitService(AppDbContext dbContext, ITenantContext tenantContext, IPlanLimitService planLimitService)
        {
            _dbContext = dbContext;
            _tenantContext = tenantContext;
            _planLimitService = planLimitService;
        }

        public async Task<UsageSummaryDto?> GetUsageAsync()
        {
            if (_tenantContext.TenantId is not { } tenantId)
            {
                return null;
            }

            var limits = await _planLimitService.GetLimitsAsync(tenantId);
            if (limits is null)
            {
                return null;
            }

            var userCount = await _dbContext.Users.CountAsync(u => u.IsActive);
            var recordCount = await GetRecordCountAsync();
            var databaseSizeMB = await GetDatabaseSizeMBAsync();

            return new UsageSummaryDto
            {
                UserCount = userCount,
                MaxUsers = limits.MaxUsers,
                RecordCount = recordCount,
                MaxRecords = limits.MaxRecords,
                DatabaseSizeMB = databaseSizeMB,
                MaxStorageMB = limits.MaxStorageMB,
            };
        }

        public async Task<string?> CheckRecordLimitAsync()
        {
            if (_tenantContext.TenantId is not { } tenantId)
            {
                return null;
            }

            var limits = await _planLimitService.GetLimitsAsync(tenantId);
            if (limits is null)
            {
                return null;
            }

            var recordCount = await GetRecordCountAsync();
            if (recordCount >= limits.MaxRecords)
            {
                return $"You've reached your plan's record limit ({limits.MaxRecords} records across leads, customers, opportunities, quotations and sales orders).";
            }

            var databaseSizeMB = await GetDatabaseSizeMBAsync();
            if (databaseSizeMB >= limits.MaxStorageMB)
            {
                return $"You've reached your plan's storage limit ({limits.MaxStorageMB} MB).";
            }

            return null;
        }

        /// <summary>Leads + Customers + Opportunities + Quotations + SalesOrders — same definition
        /// as Platform's TenantUsageDto.RecordCount, kept as five simple COUNT(*) queries (not one
        /// UNION) to match the style already established in TenantUsageService.</summary>
        private async Task<int> GetRecordCountAsync()
        {
            var leadCount = await _dbContext.Leads.CountAsync();
            var customerCount = await _dbContext.Customers.CountAsync();
            var opportunityCount = await _dbContext.Opportunities.CountAsync();
            var quotationCount = await _dbContext.Quotations.CountAsync();
            var salesOrderCount = await _dbContext.SalesOrders.CountAsync();
            return leadCount + customerCount + opportunityCount + quotationCount + salesOrderCount;
        }

        /// <summary>Data file size only (type = 0) — log file growth is maintenance overhead, not
        /// customer data volume. Identical query to TenantUsageService's Platform-side equivalent
        /// so a tenant's self-reported figure always matches what a platform admin sees.</summary>
        private async Task<decimal> GetDatabaseSizeMBAsync()
            => await _dbContext.Database
                .SqlQueryRaw<decimal>(
                    "SELECT CAST(ISNULL(SUM(size), 0) * 8.0 / 1024 AS DECIMAL(18,2)) AS Value FROM sys.database_files WHERE type = 0")
                .FirstOrDefaultAsync();
    }
}
