using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IOpportunityRepository
    {
        Task<Opportunity?> GetByIdAsync(Guid id);

        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see (Own/Team, plus any active delegations).</param>
        Task<(IReadOnlyList<Opportunity> Items, int TotalCount)> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize, AccessScope? accessScope = null);

        Task<string> GetNextOpportunityNumberAsync();

        Task AddAsync(Opportunity opportunity);

        Task UpdateAsync(Opportunity opportunity);

        Task DeleteAsync(Opportunity opportunity);

        /// <summary>Every opportunity (open and closed), for dashboard aggregation (pipeline value, win rate, stage breakdown). No paging — SMB-scale data volumes.</summary>
        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see.</param>
        Task<IReadOnlyList<Opportunity>> GetAllForDashboardAsync(AccessScope? accessScope = null);

        /// <summary>Replaces every line item on the opportunity with the given set (simple full-replace, matching ICustomerRepository's contacts/addresses pattern).</summary>
        Task ReplaceLineItemsAsync(Guid opportunityId, IEnumerable<OpportunityLineItem> lineItems);

        /// <summary>Replaces every buying-committee row on the opportunity with the given set (same full-replace convention as line items).</summary>
        Task ReplaceContactsAsync(Guid opportunityId, IEnumerable<OpportunityContact> contacts);
    }
}
