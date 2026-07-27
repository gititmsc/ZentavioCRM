using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid id);

        Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize);

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
    }
}
