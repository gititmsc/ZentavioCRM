using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ITagRepository
    {
        Task<IReadOnlyList<Tag>> GetAllAsync();

        Task<Tag?> GetByIdAsync(Guid id);

        /// <summary>Case-insensitive exact match — used to find-or-create a tag by name (e.g. from the inline "create tag" option on the picker) without relying on the caller to have looked it up first.</summary>
        Task<Tag?> GetByNameAsync(string name);

        Task AddAsync(Tag tag);

        Task UpdateAsync(Tag tag);

        Task DeleteAsync(Tag tag);

        /// <summary>How many Leads and Customers currently carry this tag — shown in the Tag Manager's delete confirmation.</summary>
        Task<(int LeadCount, int CustomerCount)> CountUsageAsync(Guid tagId);

        Task<IReadOnlyList<Tag>> GetForLeadAsync(Guid leadId);

        Task<IReadOnlyList<Tag>> GetForCustomerAsync(Guid customerId);

        /// <summary>Bulk-lookup variant for list grids — returns every Lead's tags in one query instead of N+1. Leads with no tags are simply absent from the result.</summary>
        Task<IReadOnlyDictionary<Guid, IReadOnlyList<Tag>>> GetForLeadsAsync(IReadOnlyCollection<Guid> leadIds);

        Task<IReadOnlyDictionary<Guid, IReadOnlyList<Tag>>> GetForCustomersAsync(IReadOnlyCollection<Guid> customerIds);

        /// <summary>Replaces the full set of tags on a Lead with exactly the given tag Ids (adds missing, removes extra) — the save-form semantics, not an incremental add.</summary>
        Task ReplaceLeadTagsAsync(Guid leadId, IReadOnlyCollection<Guid> tagIds);

        Task ReplaceCustomerTagsAsync(Guid customerId, IReadOnlyCollection<Guid> tagIds);
    }
}
