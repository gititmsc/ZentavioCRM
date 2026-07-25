using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ILeadRepository
    {
        Task<Lead?> GetByIdAsync(Guid id);

        Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize);

        Task<string> GetNextLeadNumberAsync();

        Task AddAsync(Lead lead);

        Task UpdateAsync(Lead lead);

        Task DeleteAsync(Lead lead);

        /// <summary>Count of leads not yet Converted, Lost, or Junk — for the dashboard's "Open Leads" card.</summary>
        Task<int> CountOpenAsync();

        /// <summary>Count of leads converted within [fromUtc, toUtcExclusive) — for the dashboard's "Converted This Month" card.</summary>
        Task<int> CountConvertedBetweenAsync(DateTime fromUtc, DateTime toUtcExclusive);
    }
}
