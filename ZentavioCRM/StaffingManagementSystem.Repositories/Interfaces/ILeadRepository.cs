using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ILeadRepository
    {
        Task<Lead?> GetByIdAsync(Guid id);

        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see (Own/Team, plus any active delegations). Null means no restriction (used by contexts, like dashboards, that intentionally bypass record-level visibility).</param>
        Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize, AccessScope? accessScope = null);

        Task<string> GetNextLeadNumberAsync();

        Task AddAsync(Lead lead);

        Task UpdateAsync(Lead lead);

        Task DeleteAsync(Lead lead);

        /// <summary>Count of leads not yet Converted, Lost, or Junk — for the dashboard's "Open Leads" card.</summary>
        /// <param name="accessScope">When non-null and Scope != All, restricts the count to records the scope's user is allowed to see.</param>
        Task<int> CountOpenAsync(AccessScope? accessScope = null);

        /// <summary>Count of leads converted within [fromUtc, toUtcExclusive) — for the dashboard's "Converted This Month" card.</summary>
        /// <param name="accessScope">When non-null and Scope != All, restricts the count to records the scope's user is allowed to see.</param>
        Task<int> CountConvertedBetweenAsync(DateTime fromUtc, DateTime toUtcExclusive, AccessScope? accessScope = null);

        /// <summary>Leads (excluding excludeLeadId) whose Email or Mobile matches either given value — used for the non-blocking duplicate warning on lead creation.</summary>
        Task<IReadOnlyList<Lead>> FindPotentialDuplicatesAsync(string? email, string? mobile, Guid? excludeLeadId);

        /// <summary>Every lead, no paging — used for CSV export. SMB-scale data volumes.</summary>
        Task<IReadOnlyList<Lead>> GetAllAsync();

        /// <summary>Open leads assigned to the user whose NextFollowUpDate has passed and haven't had a reminder sent yet.</summary>
        Task<IReadOnlyList<Lead>> GetDueForFollowUpReminderAsync(Guid userId, DateTime nowUtc);
    }
}
