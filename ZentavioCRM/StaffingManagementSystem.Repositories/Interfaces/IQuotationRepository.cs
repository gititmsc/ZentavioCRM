using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IQuotationRepository
    {
        Task<Quotation?> GetByIdAsync(Guid id);

        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see (Own/Team, plus any active delegations).</param>
        /// <param name="sortBy">Column key (case-insensitive): quotationNumber, opportunityName, customerName, grandTotal, validUntil, assignedToUserName, status, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<(IReadOnlyList<Quotation> Items, int TotalCount)> SearchAsync(
            string? search, QuotationStatus? status, Guid? opportunityId, Guid? customerId, int page, int pageSize,
            AccessScope? accessScope = null, string? sortBy = null, bool sortDescending = true);

        /// <summary>All versions of every quotation sharing this number, newest first — used to resolve "the latest version" and to list a quotation's history.</summary>
        Task<IReadOnlyList<Quotation>> GetVersionsAsync(string quotationNumber);

        Task<string> GetNextQuotationNumberAsync();

        Task<bool> HasSalesOrderAsync(Guid quotationId);

        /// <summary>Whether any quotation (any version) exists against this opportunity — used to block deleting an Opportunity that already has quotations, since Quotation.OpportunityId is a Restrict FK.</summary>
        Task<bool> HasAnyForOpportunityAsync(Guid opportunityId);

        /// <summary>Count of quotations (any version) against this customer — used to block deleting a Customer that still has quotations on it (Quotation.CustomerId is a Restrict FK).</summary>
        Task<int> CountForCustomerAsync(Guid customerId);

        Task AddAsync(Quotation quotation);

        Task UpdateAsync(Quotation quotation);

        Task DeleteAsync(Quotation quotation);

        /// <summary>Replaces every line item on the quotation with the given set (full-replace, same pattern as OpportunityLineItem).</summary>
        Task ReplaceLineItemsAsync(Guid quotationId, IEnumerable<QuotationLineItem> lineItems);
    }
}
