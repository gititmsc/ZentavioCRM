using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IProductRepository"/>
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _dbContext;

        public ProductRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Product?> GetByIdAsync(Guid id)
            => _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IReadOnlyList<Product>> GetAllActiveAsync()
            => await _dbContext.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
            string? search, string? type, string? category, bool? isActive, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            var query = _dbContext.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term) ||
                    (p.Category != null && p.Category.ToLower().Contains(term)) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<Core.Enums.ProductType>(type, true, out var parsedType))
            {
                query = query.Where(p => p.Type == parsedType);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, sortBy, sortDescending)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Column-key-driven sort, kept as an explicit switch (not reflection/dynamic-LINQ) so every
        /// sortable column is a real, EF-translatable expression. Unrecognized/null sortBy falls back
        /// to Name (ascending), matching the catalog-browsing convention used by Territory/Department/Role.
        /// </summary>
        private static IOrderedQueryable<Product> ApplySort(IQueryable<Product> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "sku" => sortDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
                "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "type" => sortDescending ? query.OrderByDescending(p => p.Type) : query.OrderBy(p => p.Type),
                "category" => sortDescending ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
                "brand" => sortDescending ? query.OrderByDescending(p => p.Brand) : query.OrderBy(p => p.Brand),
                "unitprice" => sortDescending ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
                "cost" => sortDescending ? query.OrderByDescending(p => p.Cost) : query.OrderBy(p => p.Cost),
                "isactive" => sortDescending ? query.OrderByDescending(p => p.IsActive) : query.OrderBy(p => p.IsActive),
                "createdatutc" => sortDescending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
                _ => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            };
        }

        public Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null)
            => _dbContext.Products.AnyAsync(p =>
                p.Sku.ToLower() == sku.ToLower() &&
                (excludeId == null || p.Id != excludeId));

        public async Task AddAsync(Product product)
        {
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();
        }
    }
}
