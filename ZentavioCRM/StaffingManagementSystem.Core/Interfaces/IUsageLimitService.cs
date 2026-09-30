using ZentavioCRM.Core.DTOs.Usage;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Tenant-side usage enforcement "beyond seats" — MaxUsers already has its own inline check in
    /// UserService.CreateAsync (via <see cref="IPlanLimitService"/> + IUserRepository.CountActiveAsync);
    /// this covers the other two plan limits, MaxRecords and MaxStorageMB, which every
    /// record-creating Service (Lead/Customer/Opportunity/Quotation/SalesOrder) needs to check the
    /// same way. Implemented against the request's own already-resolved AppDbContext (not a fresh
    /// ad-hoc connection like the Platform-side TenantUsageService), so it's cheap enough to call
    /// per-request rather than only from an admin dashboard.
    /// </summary>
    public interface IUsageLimitService
    {
        /// <summary>Null if there's no resolvable tenant for this request (local-dev fallback) or
        /// no matching Platform DB row.</summary>
        Task<UsageSummaryDto?> GetUsageAsync();

        /// <summary>Call right before inserting a new Lead/Customer/Opportunity/Quotation/
        /// SalesOrder. Returns null when within both the record-count and storage limits (or when
        /// there's nothing to check against); otherwise a user-facing message naming which limit
        /// was hit, ready to hand straight to ApiResponse&lt;T&gt;.FailureResponse.</summary>
        Task<string?> CheckRecordLimitAsync();
    }
}
