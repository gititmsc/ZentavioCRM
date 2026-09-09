using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Products;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IProductService"/>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IReadOnlyList<ProductDto>> GetAllActiveAsync()
        {
            var products = await _productRepository.GetAllActiveAsync();
            return products.Select(Map).ToList();
        }

        public async Task<PagedResult<ProductDto>> SearchAsync(
            string? search, string? type, string? category, bool? isActive, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _productRepository.SearchAsync(search, type, category, isActive, page, pageSize, sortBy, sortDescending);

            return new PagedResult<ProductDto>
            {
                Items = items.Select(Map).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product is null)
            {
                return ApiResponse<ProductDto>.FailureResponse("Product not found.");
            }

            return ApiResponse<ProductDto>.SuccessResponse(Map(product));
        }

        public async Task<ApiResponse<ProductDto>> CreateAsync(SaveProductRequest request)
        {
            if (await _productRepository.SkuExistsAsync(request.Sku))
            {
                return ApiResponse<ProductDto>.FailureResponse(
                    "A product with this SKU already exists.",
                    ["A product with this SKU already exists."]);
            }

            var product = new Product
            {
                Sku = request.Sku.Trim(),
                Name = request.Name.Trim(),
                Type = request.Type,
                Category = request.Category?.Trim(),
                Brand = request.Brand?.Trim(),
                UnitOfMeasure = request.UnitOfMeasure?.Trim(),
                UnitPrice = request.UnitPrice,
                Cost = request.Cost,
                TaxPercent = request.TaxPercent,
                Description = request.Description?.Trim(),
                DurationMinutes = request.DurationMinutes,
                BillingType = request.BillingType?.Trim(),
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _productRepository.AddAsync(product);

            return ApiResponse<ProductDto>.SuccessResponse(Map(product), "Product created.");
        }

        public async Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, SaveProductRequest request)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product is null)
            {
                return ApiResponse<ProductDto>.FailureResponse("Product not found.");
            }

            if (await _productRepository.SkuExistsAsync(request.Sku, id))
            {
                return ApiResponse<ProductDto>.FailureResponse(
                    "A product with this SKU already exists.",
                    ["A product with this SKU already exists."]);
            }

            product.Sku = request.Sku.Trim();
            product.Name = request.Name.Trim();
            product.Type = request.Type;
            product.Category = request.Category?.Trim();
            product.Brand = request.Brand?.Trim();
            product.UnitOfMeasure = request.UnitOfMeasure?.Trim();
            product.UnitPrice = request.UnitPrice;
            product.Cost = request.Cost;
            product.TaxPercent = request.TaxPercent;
            product.Description = request.Description?.Trim();
            product.DurationMinutes = request.DurationMinutes;
            product.BillingType = request.BillingType?.Trim();
            product.IsActive = request.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);

            return ApiResponse<ProductDto>.SuccessResponse(Map(product), "Product updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product is null)
            {
                return ApiResponse<bool>.FailureResponse("Product not found.");
            }

            await _productRepository.DeleteAsync(product);

            return ApiResponse<bool>.SuccessResponse(true, "Product deleted.");
        }

        private static ProductDto Map(Product product) => new()
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Type = product.Type.ToString(),
            Category = product.Category,
            Brand = product.Brand,
            UnitOfMeasure = product.UnitOfMeasure,
            UnitPrice = product.UnitPrice,
            Cost = product.Cost,
            TaxPercent = product.TaxPercent,
            Description = product.Description,
            DurationMinutes = product.DurationMinutes,
            BillingType = product.BillingType,
            IsActive = product.IsActive,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc,
        };
    }
}
