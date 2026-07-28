using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IOpportunityRepository
    {
        Task<Opportunity?> GetByIdAsync(Guid id);

        Task<(IReadOnlyList<Opportunity> Items, int TotalCount)> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize);

        Task<string> GetNextOpportunityNumberAsync();

        Task AddAsync(Opportunity opportunity);

        Task UpdateAsync(Opportunity opportunity);

        Task DeleteAsync(Opportunity opportunity);

        /// <summary>Every opportunity (open and closed), for dashboard aggregation (pipeline value, win rate, stage breakdown). No paging — SMB-scale data volumes.</summary>
        Task<IReadOnlyList<Opportunity>> GetAllForDashboardAsync();

        /// <summary>Replaces every line item on the opportunity with the given set (simple full-replace, matching ICustomerRepository's contacts/addresses pattern).</summary>
        Task ReplaceLineItemsAsync(Guid opportunityId, IEnumerable<OpportunityLineItem> lineItems);

        /// <summary>Replaces every buying-committee row on the opportunity with the given set (same full-replace convention as line items).</summary>
        Task ReplaceContactsAsync(Guid opportunityId, IEnumerable<OpportunityContact> contacts);
    }
}
