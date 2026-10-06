using ZentavioCRM.Core.Analytics;
using ZentavioCRM.Core.DTOs.Dashboard;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IOpportunityRepository
    {
        Task<Opportunity?> GetByIdAsync(Guid id);

        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see (Own/Team, plus any active delegations).</param>
        /// <param name="sortBy">Column key (case-insensitive): opportunityNumber, name, customerName, value, expectedCloseDate, assignedToUserName, stage, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<(IReadOnlyList<Opportunity> Items, int TotalCount)> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize,
            AccessScope? accessScope = null, string? sortBy = null, bool sortDescending = true);

        Task<string> GetNextOpportunityNumberAsync();

        /// <summary>Flat rows for the analytics engine / report builder: every opportunity the caller's scope allows (date filtering happens in the engine).</summary>
        /// <param name="mineUserId">When set, additionally restricts to opportunities assigned to this user.</param>
        Task<IReadOnlyList<AnalyticsRecord>> GetAnalyticsRecordsAsync(Guid? mineUserId, AccessScope? accessScope = null);

        Task AddAsync(Opportunity opportunity);

        Task UpdateAsync(Opportunity opportunity);

        Task DeleteAsync(Opportunity opportunity);

        /// <summary>Every opportunity (open and closed), for dashboard aggregation (pipeline value, win rate, stage breakdown). No paging — SMB-scale data volumes.</summary>
        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see.</param>
        Task<IReadOnlyList<Opportunity>> GetAllForDashboardAsync(AccessScope? accessScope = null);

        /// <summary>Lightweight rows for every opportunity CREATED or CLOSED inside [fromUtc, toUtcExclusive) — callers filter the two cohorts apart in memory.</summary>
        /// <param name="mineUserId">When set, additionally restricts to opportunities assigned to this user.</param>
        Task<IReadOnlyList<DashboardOpportunityRow>> GetForDashboardAsync(DateTime fromUtc, DateTime toUtcExclusive, Guid? mineUserId, AccessScope? accessScope = null);

        /// <summary>Replaces every line item on the opportunity with the given set (simple full-replace, matching ICustomerRepository's contacts/addresses pattern).</summary>
        Task ReplaceLineItemsAsync(Guid opportunityId, IEnumerable<OpportunityLineItem> lineItems);

        /// <summary>Replaces every buying-committee row on the opportunity with the given set (same full-replace convention as line items).</summary>
        Task ReplaceContactsAsync(Guid opportunityId, IEnumerable<OpportunityContact> contacts);

        /// <summary>Count of opportunities against this customer — used to block deleting a Customer that still has deals on it (Opportunity.CustomerId is a Restrict FK, so this would otherwise surface as a raw DB error).</summary>
        Task<int> CountForCustomerAsync(Guid customerId);
    }
}
