using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Products;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IProductService
    {
        /// <summary>Unpaged, active-only list — powers the "pick from catalog" convenience selector on Opportunity/Quotation line items.</summary>
        Task<IReadOnlyList<ProductDto>> GetAllActiveAsync();

        /// <summary>Paged, filterable, sortable product search — powers the Product Catalog administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): sku, name, type, category, brand, unitprice, cost, isactive, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<PagedResult<ProductDto>> SearchAsync(
            string? search, string? type, string? category, bool? isActive, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<ProductDto>> CreateAsync(SaveProductRequest request);

        Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, SaveProductRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
