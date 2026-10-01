using ZentavioCRM.Core.DTOs.Common;

namespace ZentavioCRM.Repositories.Interfaces
{
    /// <summary>
    /// Cross-entity merge logic for duplicate Leads/Customers. Lives outside ILeadRepository/
    /// ICustomerRepository because a merge touches many unrelated tables (Activities, Opportunities,
    /// Quotations, Sales Orders, Contacts, Addresses, Tags...) in one transaction — it isn't owned
    /// by any single entity's repository.
    /// </summary>
    public interface IMergeRepository
    {
        /// <summary>Folds <paramref name="losingLeadId"/> into <paramref name="survivingLeadId"/>: repoints Activities/Opportunity.SourceLeadId, merges LeadTags, then hard-deletes the losing lead (Lead has no soft-delete/archive flag).</summary>
        Task<MergeResultDto> MergeLeadsAsync(Guid survivingLeadId, Guid losingLeadId);

        /// <summary>Folds <paramref name="losingCustomerId"/> into <paramref name="survivingCustomerId"/>: repoints Opportunities/Quotations/SalesOrders/Contacts/Addresses/Activities/CustomerTags and any Lead.LinkedCustomerId/ConvertedCustomerId, then archives the losing customer (IsActive=false) rather than deleting it.</summary>
        Task<MergeResultDto> MergeCustomersAsync(Guid survivingCustomerId, Guid losingCustomerId);
    }
}
