using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Quotations;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IQuotationService
    {
        /// <param name="sortBy">Column key (case-insensitive): quotationNumber, opportunityName, customerName, grandTotal, validUntil, assignedToUserName, status, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<PagedResult<QuotationListItemDto>> SearchAsync(
            string? search, QuotationStatus? status, Guid? opportunityId, Guid? customerId, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true);

        Task<ApiResponse<QuotationDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<QuotationDto>> CreateAsync(CreateQuotationRequest request, Guid? currentUserId);

        Task<ApiResponse<QuotationDto>> UpdateAsync(Guid id, UpdateQuotationRequest request, Guid? currentUserId);

        Task<ApiResponse<QuotationDto>> UpdateStatusAsync(Guid id, UpdateQuotationStatusRequest request, Guid? currentUserId);

        Task<ApiResponse<QuotationDto>> AssignAsync(Guid id, AssignQuotationRequest request, Guid? currentUserId);

        /// <summary>Clones the given quotation's line items into a new Draft row with Version + 1 — used once a quotation has already been sent and needs a re-quote instead of an in-place edit.</summary>
        Task<ApiResponse<QuotationDto>> CreateNewVersionAsync(Guid id, Guid? currentUserId);

        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId);
    }
}
