using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;
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
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize,
            AccessScope? accessScope = null, string? sortBy = null, bool sortDescending = true)
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

            if (accessScope is not null && accessScope.Scope != VisibilityScope.All)
            {
                var currentUserId = accessScope.UserId;
                // .ToHashSet() materializes to a concrete HashSet<Guid> (ICollection<Guid>) — EF Core's
                // Contains -> SQL IN translation is resolved from the compile-time type, and the source
                // properties are declared as IReadOnlySet<Guid>, which isn't covered by that translation.
                var teamIds = accessScope.TeamUserIds.ToHashSet();
                var delegatedIds = accessScope.DelegatedFromUserIds.ToHashSet();

                query = accessScope.Scope == VisibilityScope.Team
                    ? query.Where(c =>
                        (c.AssignedToUserId != null && teamIds.Contains(c.AssignedToUserId.Value)) ||
                        (c.AssignedToUserId == null && c.CreatedByUserId != null && teamIds.Contains(c.CreatedByUserId.Value)) ||
                        (c.AssignedToUserId != null && delegatedIds.Contains(c.AssignedToUserId.Value)))
                    : query.Where(c =>
                        c.AssignedToUserId == currentUserId ||
                        (c.AssignedToUserId == null && c.CreatedByUserId == currentUserId) ||
                        (c.AssignedToUserId != null && delegatedIds.Contains(c.AssignedToUserId.Value)));
            }

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, sortBy, sortDescending)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Column-key-driven sort, kept as an explicit switch (not reflection/dynamic-LINQ) so every
        /// sortable column is a real, EF-translatable expression. Unrecognized/null sortBy falls back to CreatedAtUtc.
        /// </summary>
        private static IOrderedQueryable<Customer> ApplySort(IQueryable<Customer> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "customernumber" => sortDescending ? query.OrderByDescending(c => c.CustomerNumber) : query.OrderBy(c => c.CustomerNumber),
                "displayname" => sortDescending ? query.OrderByDescending(c => c.DisplayName) : query.OrderBy(c => c.DisplayName),
                "type" => sortDescending ? query.OrderByDescending(c => c.Type) : query.OrderBy(c => c.Type),
                "industry" => sortDescending ? query.OrderByDescending(c => c.Industry) : query.OrderBy(c => c.Industry),
                "healthstatus" => sortDescending ? query.OrderByDescending(c => c.HealthStatus) : query.OrderBy(c => c.HealthStatus),
                "assignedtousername" => sortDescending
                    ? query.OrderByDescending(c => c.AssignedToUser!.FirstName).ThenByDescending(c => c.AssignedToUser!.LastName)
                    : query.OrderBy(c => c.AssignedToUser!.FirstName).ThenBy(c => c.AssignedToUser!.LastName),
                "isactive" => sortDescending ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
                _ => sortDescending ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc),
            };
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

        /// <remarks>
        /// Note: since this is a full replace (not a merge), BirthdayReminderSentYear/AnniversaryReminderSentYear
        /// reset to null on every save of the parent Customer — a resaved contact could theoretically get a
        /// duplicate birthday/anniversary notification if the Customer is edited again later the same day.
        /// Accepted as a minor edge case rather than adding contact-matching logic to preserve it.
        /// </remarks>
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

        public async Task<IReadOnlyList<Customer>> FindByEmailOrPhoneAsync(string? email, string? phone)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            {
                return [];
            }

            var normalizedEmail = email?.Trim().ToLower();
            var normalizedPhone = phone?.Trim();

            var matches = await _dbContext.Customers
                .Where(c =>
                    (normalizedEmail != null && c.Email != null && c.Email.ToLower() == normalizedEmail) ||
                    (normalizedPhone != null && c.Phone != null && c.Phone == normalizedPhone))
                .ToListAsync();

            return matches;
        }

        public async Task<IReadOnlyList<Customer>> GetAllAsync()
        {
            var customers = await _dbContext.Customers
                .Include(c => c.AssignedToUser)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();

            return customers;
        }

        public async Task<IReadOnlyList<ContactPerson>> GetDueForBirthdayReminderAsync(Guid userId, DateTime nowUtc)
            => await _dbContext.ContactPersons
                .Include(cp => cp.Customer)
                .Where(cp =>
                    cp.Customer!.AssignedToUserId == userId &&
                    cp.DateOfBirth != null &&
                    cp.DateOfBirth.Value.Month == nowUtc.Month &&
                    cp.DateOfBirth.Value.Day == nowUtc.Day &&
                    cp.BirthdayReminderSentYear != nowUtc.Year)
                .ToListAsync();

        public async Task<IReadOnlyList<ContactPerson>> GetDueForAnniversaryReminderAsync(Guid userId, DateTime nowUtc)
            => await _dbContext.ContactPersons
                .Include(cp => cp.Customer)
                .Where(cp =>
                    cp.Customer!.AssignedToUserId == userId &&
                    cp.AnniversaryDate != null &&
                    cp.AnniversaryDate.Value.Month == nowUtc.Month &&
                    cp.AnniversaryDate.Value.Day == nowUtc.Day &&
                    cp.AnniversaryReminderSentYear != nowUtc.Year)
                .ToListAsync();

        public async Task UpdateContactAsync(ContactPerson contact)
        {
            _dbContext.ContactPersons.Update(contact);
            await _dbContext.SaveChangesAsync();
        }
    }
}
