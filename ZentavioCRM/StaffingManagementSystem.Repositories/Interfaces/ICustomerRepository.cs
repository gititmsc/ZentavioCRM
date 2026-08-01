using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid id);

        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see (Own/Team, plus any active delegations).</param>
        /// <param name="sortBy">Column key (case-insensitive): customerNumber, displayName, type, industry, healthStatus, assignedToUserName, isActive, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize,
            AccessScope? accessScope = null, string? sortBy = null, bool sortDescending = true);

        Task<string> GetNextCustomerNumberAsync();

        Task AddAsync(Customer customer);

        Task UpdateAsync(Customer customer);

        Task DeleteAsync(Customer customer);

        /// <summary>Replaces every contact on the customer with the given set (simple full-replace — contacts have no external references).</summary>
        Task ReplaceContactsAsync(Guid customerId, IEnumerable<ContactPerson> contacts);

        /// <summary>Replaces every address on the customer with the given set.</summary>
        Task ReplaceAddressesAsync(Guid customerId, IEnumerable<CustomerAddress> addresses);

        /// <summary>Customers whose Email or Phone matches either given value — used for the lead duplicate-check.</summary>
        Task<IReadOnlyList<Customer>> FindByEmailOrPhoneAsync(string? email, string? phone);

        /// <summary>Every customer, no paging — used for CSV export. SMB-scale data volumes.</summary>
        Task<IReadOnlyList<Customer>> GetAllAsync();

        /// <summary>Contacts (with Customer loaded) whose Customer is assigned to the user and whose DateOfBirth's month/day matches today, and no reminder has been sent yet this calendar year.</summary>
        Task<IReadOnlyList<ContactPerson>> GetDueForBirthdayReminderAsync(Guid userId, DateTime nowUtc);

        /// <summary>Contacts (with Customer loaded) whose Customer is assigned to the user and whose AnniversaryDate's month/day matches today, and no reminder has been sent yet this calendar year.</summary>
        Task<IReadOnlyList<ContactPerson>> GetDueForAnniversaryReminderAsync(Guid userId, DateTime nowUtc);

        /// <summary>Persists changes to a single contact — used to stamp the reminder-sent-year fields after a birthday/anniversary notification is sent.</summary>
        Task UpdateContactAsync(ContactPerson contact);
    }
}
