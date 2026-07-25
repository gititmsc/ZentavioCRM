using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Customers;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ICustomerService"/>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<PagedResult<CustomerListItemDto>> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _customerRepository.SearchAsync(search, assignedToUserId, isActive, page, pageSize);

            return new PagedResult<CustomerListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<CustomerDto>> GetByIdAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            return customer is null
                ? ApiResponse<CustomerDto>.FailureResponse("Customer not found.")
                : ApiResponse<CustomerDto>.SuccessResponse(Map(customer));
        }

        public async Task<ApiResponse<CustomerDto>> CreateAsync(SaveCustomerRequest request)
        {
            var customer = new Customer
            {
                CustomerNumber = await _customerRepository.GetNextCustomerNumberAsync(),
                Type = request.Type,
                LegalName = request.LegalName.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.LegalName.Trim() : request.DisplayName.Trim(),
                Industry = request.Industry,
                Website = request.Website,
                Email = request.Email,
                Phone = request.Phone,
                TaxNumber = request.TaxNumber,
                EmployeesCount = request.EmployeesCount,
                AnnualRevenue = request.AnnualRevenue,
                CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode,
                PaymentTermsDays = request.PaymentTermsDays,
                CreditLimit = request.CreditLimit,
                Rating = request.Rating,
                AssignedToUserId = request.AssignedToUserId,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _customerRepository.AddAsync(customer);
            await SyncChildCollectionsAsync(customer.Id, request);

            var created = await _customerRepository.GetByIdAsync(customer.Id);
            return ApiResponse<CustomerDto>.SuccessResponse(Map(created!), "Customer created.");
        }

        public async Task<ApiResponse<CustomerDto>> UpdateAsync(Guid id, SaveCustomerRequest request)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer is null)
            {
                return ApiResponse<CustomerDto>.FailureResponse("Customer not found.");
            }

            customer.Type = request.Type;
            customer.LegalName = request.LegalName.Trim();
            customer.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.LegalName.Trim() : request.DisplayName.Trim();
            customer.Industry = request.Industry;
            customer.Website = request.Website;
            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.TaxNumber = request.TaxNumber;
            customer.EmployeesCount = request.EmployeesCount;
            customer.AnnualRevenue = request.AnnualRevenue;
            customer.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode;
            customer.PaymentTermsDays = request.PaymentTermsDays;
            customer.CreditLimit = request.CreditLimit;
            customer.Rating = request.Rating;
            customer.AssignedToUserId = request.AssignedToUserId;
            customer.IsActive = request.IsActive;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(customer);
            await SyncChildCollectionsAsync(id, request);

            var updated = await _customerRepository.GetByIdAsync(id);
            return ApiResponse<CustomerDto>.SuccessResponse(Map(updated!), "Customer updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer is null)
            {
                return ApiResponse<bool>.FailureResponse("Customer not found.");
            }

            await _customerRepository.DeleteAsync(customer);
            return ApiResponse<bool>.SuccessResponse(true, "Customer deleted.");
        }

        private async Task SyncChildCollectionsAsync(Guid customerId, SaveCustomerRequest request)
        {
            var contacts = request.Contacts.Select(c => new ContactPerson
            {
                FirstName = c.FirstName.Trim(),
                LastName = c.LastName?.Trim() ?? string.Empty,
                Designation = c.Designation,
                Department = c.Department,
                Email = c.Email,
                Mobile = c.Mobile,
                WhatsApp = c.WhatsApp,
                LinkedIn = c.LinkedIn,
                IsPrimary = c.IsPrimary,
                IsDecisionMaker = c.IsDecisionMaker,
                Notes = c.Notes,
            });
            await _customerRepository.ReplaceContactsAsync(customerId, contacts);

            var addresses = request.Addresses.Select(a => new CustomerAddress
            {
                Type = a.Type,
                Line1 = a.Line1.Trim(),
                Line2 = a.Line2,
                City = a.City,
                State = a.State,
                Country = a.Country,
                PostalCode = a.PostalCode,
                IsPrimary = a.IsPrimary,
            });
            await _customerRepository.ReplaceAddressesAsync(customerId, addresses);
        }

        private static CustomerListItemDto MapListItem(Customer customer) => new()
        {
            Id = customer.Id,
            CustomerNumber = customer.CustomerNumber,
            Type = customer.Type,
            DisplayName = customer.DisplayName,
            Industry = customer.Industry,
            Email = customer.Email,
            Phone = customer.Phone,
            AssignedToUserName = customer.AssignedToUser?.FullName,
            IsActive = customer.IsActive,
            CreatedAtUtc = customer.CreatedAtUtc,
        };

        private static CustomerDto Map(Customer customer) => new()
        {
            Id = customer.Id,
            CustomerNumber = customer.CustomerNumber,
            Type = customer.Type,
            LegalName = customer.LegalName,
            DisplayName = customer.DisplayName,
            Industry = customer.Industry,
            Website = customer.Website,
            Email = customer.Email,
            Phone = customer.Phone,
            TaxNumber = customer.TaxNumber,
            EmployeesCount = customer.EmployeesCount,
            AnnualRevenue = customer.AnnualRevenue,
            CurrencyCode = customer.CurrencyCode,
            PaymentTermsDays = customer.PaymentTermsDays,
            CreditLimit = customer.CreditLimit,
            Rating = customer.Rating,
            AssignedToUserId = customer.AssignedToUserId,
            AssignedToUserName = customer.AssignedToUser?.FullName,
            IsActive = customer.IsActive,
            CreatedAtUtc = customer.CreatedAtUtc,
            Contacts = customer.Contacts.Select(c => new ContactPersonDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Designation = c.Designation,
                Department = c.Department,
                Email = c.Email,
                Mobile = c.Mobile,
                WhatsApp = c.WhatsApp,
                LinkedIn = c.LinkedIn,
                IsPrimary = c.IsPrimary,
                IsDecisionMaker = c.IsDecisionMaker,
                Notes = c.Notes,
            }).ToList(),
            Addresses = customer.Addresses.Select(a => new CustomerAddressDto
            {
                Id = a.Id,
                Type = a.Type,
                Line1 = a.Line1,
                Line2 = a.Line2,
                City = a.City,
                State = a.State,
                Country = a.Country,
                PostalCode = a.PostalCode,
                IsPrimary = a.IsPrimary,
            }).ToList(),
        };
    }
}
