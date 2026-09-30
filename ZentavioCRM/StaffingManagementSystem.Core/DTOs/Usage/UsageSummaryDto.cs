namespace ZentavioCRM.Core.DTOs.Usage
{
    /// <summary>
    /// Live usage vs. plan limits for the CURRENT tenant — the self-service counterpart to
    /// Platform's <see cref="Platform.TenantUsageDto"/> (which any platform admin can pull for any
    /// tenant). This one is scoped to whichever tenant the calling request resolved to, computed
    /// from the request's own already-connected AppDbContext rather than opening a new one, so it's
    /// cheap enough to call on every page load for the in-app usage banner.
    /// </summary>
    public class UsageSummaryDto
    {
        public int UserCount { get; set; }

        public int MaxUsers { get; set; }

        /// <summary>Leads + Customers + Opportunities + Quotations + SalesOrders combined — same
        /// definition as Platform's TenantUsageDto.RecordCount.</summary>
        public int RecordCount { get; set; }

        public int MaxRecords { get; set; }

        public decimal DatabaseSizeMB { get; set; }

        public int MaxStorageMB { get; set; }
    }
}
