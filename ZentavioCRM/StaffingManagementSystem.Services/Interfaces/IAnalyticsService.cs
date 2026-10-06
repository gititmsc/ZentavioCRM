using ZentavioCRM.Core.Analytics;
using ZentavioCRM.Core.DTOs.Analytics;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>
    /// Generic, whitelisted group-and-aggregate engine behind custom dashboard widgets and the report builder.
    /// Callers (the controller) are responsible for the per-entity View permission check; every method here
    /// additionally restricts records to the user's Role.VisibilityScope. Malformed or non-whitelisted input
    /// raises <see cref="AnalyticsQueryException"/>.
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>The entities/fields the UI may offer, restricted to the entities the caller can view.</summary>
        IReadOnlyList<AnalyticsCatalogEntityDto> GetCatalog(IReadOnlySet<AnalyticsEntity> allowedEntities);

        Task<AnalyticsQueryResult> QueryAsync(AnalyticsQueryRequest request, Guid userId);

        Task<AnalyticsRowsResult> RowsAsync(AnalyticsRowsRequest request, Guid userId);

        /// <summary>Same query as <see cref="RowsAsync"/> (row limit forced to the maximum), rendered as an .xlsx file.</summary>
        Task<byte[]> ExportXlsxAsync(AnalyticsRowsRequest request, Guid userId);

        /// <summary>Same query as <see cref="RowsAsync"/> (row limit forced to the maximum), rendered as CSV text.</summary>
        Task<string> ExportCsvAsync(AnalyticsRowsRequest request, Guid userId);
    }
}
