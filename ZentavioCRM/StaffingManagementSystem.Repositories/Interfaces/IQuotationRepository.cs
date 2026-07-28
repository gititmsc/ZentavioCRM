using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IQuotationRepository
    {
        Task<Quotation?> GetByIdAsync(Guid id);

        Task<(IReadOnlyList<Quotation> Items, int TotalCount)> SearchAsync(
            string? search, QuotationStatus? status, Guid? opportunityId, Guid? customerId, int page, int pageSize);

        /// <summary>All versions of every quotation sharing this number, newest first — used to resolve "the latest version" and to list a quotation's history.</summary>
        Task<IReadOnlyList<Quotation>> GetVersionsAsync(string quotationNumber);

        Task<string> GetNextQuotationNumberAsync();

        Task<bool> HasSalesOrderAsync(Guid quotationId);

        Task AddAsync(Quotation quotation);

        Task UpdateAsync(Quotation quotation);

        Task DeleteAsync(Quotation quotation);

        /// <summary>Replaces every line item on the quotation with the given set (full-replace, same pattern as OpportunityLineItem).</summary>
        Task ReplaceLineItemsAsync(Guid quotationId, IEnumerable<QuotationLineItem> lineItems);
    }
}
