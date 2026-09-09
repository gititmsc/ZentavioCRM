using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);

        /// <summary>Unpaged, active-first list — powers the "pick from catalog" convenience selector on Opportunity/Quotation line items.</summary>
        Task<IReadOnlyList<Product>> GetAllActiveAsync();

        /// <summary>Paged, filterable, sortable product search — powers the Product Catalog administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): sku, name, type, category, brand, unitprice, cost, isactive, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
            string? search, string? type, string? category, bool? isActive, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null);

        Task AddAsync(Product product);

        Task UpdateAsync(Product product);

        Task DeleteAsync(Product product);
    }
}
