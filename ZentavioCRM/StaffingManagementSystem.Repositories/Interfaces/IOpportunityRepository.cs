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
    }
}
