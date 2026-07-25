using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ICustomerRepository"/>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _dbContext;

        public CustomerRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IQueryable<Customer> WithDetails() => _dbContext.Customers
            .Include(c => c.AssignedToUser)
            .Include(c => c.Contacts)
            .Include(c => c.Addresses);

        public Task<Customer?> GetByIdAsync(Guid id)
            => WithDetails().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize)
        {
            var query = _dbContext.Customers.Include(c => c.AssignedToUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.DisplayName.ToLower().Contains(term) ||
                    c.LegalName.ToLower().Contains(term) ||
                    c.CustomerNumber.ToLower().Contains(term) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)));
            }

            if (assignedToUserId is not null)
            {
                query = query.Where(c => c.AssignedToUserId == assignedToUserId);
            }

            if (isActive is not null)
            {
                query = query.Where(c => c.IsActive == isActive);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<string> GetNextCustomerNumberAsync()
        {
            var count = await _dbContext.Customers.CountAsync();
            return $"CUST-{count + 1:000000}";
        }

        public async Task AddAsync(Customer customer)
        {
            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            _dbContext.Customers.Update(customer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Customer customer)
        {
            _dbContext.Customers.Remove(customer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceContactsAsync(Guid customerId, IEnumerable<ContactPerson> contacts)
        {
            var existing = await _dbContext.ContactPersons.Where(cp => cp.CustomerId == customerId).ToListAsync();
            _dbContext.ContactPersons.RemoveRange(existing);

            foreach (var contact in contacts)
            {
                contact.Id = Guid.Empty;
                contact.CustomerId = customerId;
                contact.CreatedAtUtc = DateTime.UtcNow;
                _dbContext.ContactPersons.Add(contact);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceAddressesAsync(Guid customerId, IEnumerable<CustomerAddress> addresses)
        {
            var existing = await _dbContext.CustomerAddresses.Where(a => a.CustomerId == customerId).ToListAsync();
            _dbContext.CustomerAddresses.RemoveRange(existing);

            foreach (var address in addresses)
            {
                address.Id = Guid.Empty;
                address.CustomerId = customerId;
                _dbContext.CustomerAddresses.Add(address);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
