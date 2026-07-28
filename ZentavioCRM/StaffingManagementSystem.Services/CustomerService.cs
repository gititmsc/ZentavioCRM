using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Core.DTOs.Customers;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ICustomerService"/>
    public class CustomerService : ICustomerService
    {
        private const string EntityType = "Customer";

        private readonly ICustomerRepository _customerRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IAccessScopeService _accessScopeService;

        public CustomerService(ICustomerRepository customerRepository, IAuditLogService auditLogService, IAccessScopeService accessScopeService)
        {
            _customerRepository = customerRepository;
            _auditLogService = auditLogService;
            _accessScopeService = accessScopeService;
        }

        /// <summary>In-memory record-visibility check for a single already-fetched Customer. Returns true (no restriction) when currentUserId is null, since that only happens for internal/system callers, never an authenticated HTTP request.</summary>
        private async Task<bool> CanAccessAsync(Guid? currentUserId, Customer customer)
        {
            if (currentUserId is null)
            {
                return true;
            }

            var scope = await _accessScopeService.GetForUserAsync(currentUserId.Value);
            return scope.CanSee(customer.AssignedToUserId, customer.CreatedByUserId);
        }

        public async Task<PagedResult<CustomerListItemDto>> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize, Guid? currentUserId = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            AccessScope? accessScope = currentUserId is null ? null : await _accessScopeService.GetForUserAsync(currentUserId.Value);
            var (items, totalCount) = await _customerRepository.SearchAsync(search, assignedToUserId, isActive, page, pageSize, accessScope);

            return new PagedResult<CustomerListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<CustomerDto>> GetByIdAsync(Guid id, Guid? currentUserId = null)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer is null)
            {
                return ApiResponse<CustomerDto>.FailureResponse("Customer not found.");
            }

            if (!await CanAccessAsync(currentUserId, customer))
            {
                return ApiResponse<CustomerDto>.FailureResponse("Customer not found.");
            }

            return ApiResponse<CustomerDto>.SuccessResponse(Map(customer));
        }

        public async Task<ApiResponse<CustomerDto>> CreateAsync(SaveCustomerRequest request, Guid? currentUserId)
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
                Tags = request.Tags,
                AcquisitionSource = request.AcquisitionSource,
                HealthStatus = request.HealthStatus,
                AssignedToUserId = request.AssignedToUserId,
                CreatedByUserId = currentUserId,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _customerRepository.AddAsync(customer);
            await SyncChildCollectionsAsync(customer.Id, request);
            await _auditLogService.LogAsync(EntityType, customer.Id, "Created", $"Customer {customer.CustomerNumber} created.", currentUserId);

            var created = await _customerRepository.GetByIdAsync(customer.Id);
            return ApiResponse<CustomerDto>.SuccessResponse(Map(created!), "Customer created.");
        }

        public async Task<ApiResponse<CustomerDto>> UpdateAsync(Guid id, SaveCustomerRequest request, Guid? currentUserId)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer is null)
            {
                return ApiResponse<CustomerDto>.FailureResponse("Customer not found.");
            }

            if (!await CanAccessAsync(currentUserId, customer))
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
            customer.Tags = request.Tags;
            customer.AcquisitionSource = request.AcquisitionSource;
            customer.HealthStatus = request.HealthStatus;
            customer.AssignedToUserId = request.AssignedToUserId;
            customer.IsActive = request.IsActive;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            await _customerRepository.UpdateAsync(customer);
            await SyncChildCollectionsAsync(id, request);
            await _auditLogService.LogAsync(EntityType, id, "Updated", "Customer details updated.", currentUserId);

            var updated = await _customerRepository.GetByIdAsync(id);
            return ApiResponse<CustomerDto>.SuccessResponse(Map(updated!), "Customer updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer is null)
            {
                return ApiResponse<bool>.FailureResponse("Customer not found.");
            }

            if (!await CanAccessAsync(currentUserId, customer))
            {
                return ApiResponse<bool>.FailureResponse("Customer not found.");
            }

            await _customerRepository.DeleteAsync(customer);
            await _auditLogService.LogAsync(EntityType, id, "Deleted", $"Customer {customer.CustomerNumber} deleted.", currentUserId);
            return ApiResponse<bool>.SuccessResponse(true, "Customer deleted.");
        }

        private static readonly string[] ExportHeaders =
        [
            "CustomerNumber", "Type", "LegalName", "DisplayName", "Industry", "Website", "Email", "Phone",
            "TaxNumber", "EmployeesCount", "AnnualRevenue", "CurrencyCode", "PaymentTermsDays", "CreditLimit",
            "Rating", "Tags", "AcquisitionSource", "HealthStatus", "AssignedToUserName", "IsActive", "CreatedAtUtc",
        ];

        /// <summary>Columns accepted on import — a subset of the export columns: system-managed fields (CustomerNumber, AssignedToUserName, CreatedAtUtc) are not importable. Contacts/Addresses are not importable via CSV in this milestone — add them afterward on the Customer's edit screen.</summary>
        private static readonly string[] ImportHeaders =
        [
            "Type", "LegalName", "DisplayName", "Industry", "Website", "Email", "Phone", "TaxNumber",
            "EmployeesCount", "AnnualRevenue", "CurrencyCode", "PaymentTermsDays", "CreditLimit", "Rating",
            "Tags", "AcquisitionSource", "HealthStatus", "IsActive",
        ];

        public async Task<string> ExportCsvAsync()
        {
            var customers = await _customerRepository.GetAllAsync();

            var rows = customers.Select(c => (IReadOnlyList<string?>)new List<string?>
            {
                c.CustomerNumber,
                c.Type.ToString(),
                c.LegalName,
                c.DisplayName,
                c.Industry,
                c.Website,
                c.Email,
                c.Phone,
                c.TaxNumber,
                c.EmployeesCount?.ToString(),
                c.AnnualRevenue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                c.CurrencyCode,
                c.PaymentTermsDays?.ToString(),
                c.CreditLimit?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                c.Rating,
                c.Tags,
                c.AcquisitionSource?.ToString(),
                c.HealthStatus?.ToString(),
                c.AssignedToUser?.FullName,
                c.IsActive.ToString(),
                c.CreatedAtUtc.ToString("O"),
            });

            return CsvUtility.Write(ExportHeaders, rows);
        }

        public async Task<ImportResultDto> ImportCsvAsync(string csvContent, Guid? currentUserId)
        {
            var (headers, rows) = CsvUtility.Parse(csvContent);
            var result = new ImportResultDto { TotalRows = rows.Count };

            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                columnIndex[headers[i].Trim()] = i;
            }

            if (!columnIndex.ContainsKey("LegalName"))
            {
                result.Errors.Add(new ImportRowErrorDto { RowNumber = 0, Message = "CSV is missing required column: LegalName." });
                return result;
            }

            string? Get(string[] row, string column)
                => columnIndex.TryGetValue(column, out var idx) && idx < row.Length && !string.IsNullOrWhiteSpace(row[idx]) ? row[idx].Trim() : null;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 1;
                var row = rows[i];

                try
                {
                    var legalName = Get(row, "LegalName");
                    if (string.IsNullOrWhiteSpace(legalName))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = "LegalName is required." });
                        result.FailureCount++;
                        continue;
                    }

                    var typeRaw = Get(row, "Type");
                    var type = typeRaw is not null && Enum.TryParse<CustomerType>(typeRaw, true, out var parsedType)
                        ? parsedType
                        : CustomerType.Prospect;

                    int? employeesCount = null;
                    var employeesCountRaw = Get(row, "EmployeesCount");
                    if (employeesCountRaw is not null && !int.TryParse(employeesCountRaw, out var parsedEmployeesCount))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = $"EmployeesCount '{employeesCountRaw}' is not a valid whole number." });
                        result.FailureCount++;
                        continue;
                    }
                    else if (employeesCountRaw is not null)
                    {
                        employeesCount = int.Parse(employeesCountRaw);
                    }

                    decimal? annualRevenue = null;
                    var annualRevenueRaw = Get(row, "AnnualRevenue");
                    if (annualRevenueRaw is not null && !decimal.TryParse(annualRevenueRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedAnnualRevenue))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = $"AnnualRevenue '{annualRevenueRaw}' is not a valid number." });
                        result.FailureCount++;
                        continue;
                    }
                    else if (annualRevenueRaw is not null)
                    {
                        annualRevenue = decimal.Parse(annualRevenueRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    int? paymentTermsDays = null;
                    var paymentTermsDaysRaw = Get(row, "PaymentTermsDays");
                    if (paymentTermsDaysRaw is not null && !int.TryParse(paymentTermsDaysRaw, out var parsedPaymentTermsDays))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = $"PaymentTermsDays '{paymentTermsDaysRaw}' is not a valid whole number." });
                        result.FailureCount++;
                        continue;
                    }
                    else if (paymentTermsDaysRaw is not null)
                    {
                        paymentTermsDays = int.Parse(paymentTermsDaysRaw);
                    }

                    decimal? creditLimit = null;
                    var creditLimitRaw = Get(row, "CreditLimit");
                    if (creditLimitRaw is not null && !decimal.TryParse(creditLimitRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedCreditLimit))
                    {
                        result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = $"CreditLimit '{creditLimitRaw}' is not a valid number." });
                        result.FailureCount++;
                        continue;
                    }
                    else if (creditLimitRaw is not null)
                    {
                        creditLimit = decimal.Parse(creditLimitRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    var displayName = Get(row, "DisplayName") ?? legalName;
                    var isActiveRaw = Get(row, "IsActive");
                    var isActive = isActiveRaw is null || !bool.TryParse(isActiveRaw, out var parsedIsActive) || parsedIsActive;

                    var acquisitionSourceRaw = Get(row, "AcquisitionSource");
                    LeadSource? acquisitionSource = acquisitionSourceRaw is not null && Enum.TryParse<LeadSource>(acquisitionSourceRaw, true, out var parsedAcquisitionSource)
                        ? parsedAcquisitionSource
                        : null;

                    var healthStatusRaw = Get(row, "HealthStatus");
                    CustomerHealthStatus? healthStatus = healthStatusRaw is not null && Enum.TryParse<CustomerHealthStatus>(healthStatusRaw, true, out var parsedHealthStatus)
                        ? parsedHealthStatus
                        : null;

                    var customer = new Customer
                    {
                        CustomerNumber = await _customerRepository.GetNextCustomerNumberAsync(),
                        Type = type,
                        LegalName = legalName.Trim(),
                        DisplayName = displayName.Trim(),
                        Industry = Get(row, "Industry"),
                        Website = Get(row, "Website"),
                        Email = Get(row, "Email"),
                        Phone = Get(row, "Phone"),
                        TaxNumber = Get(row, "TaxNumber"),
                        EmployeesCount = employeesCount,
                        AnnualRevenue = annualRevenue,
                        CurrencyCode = Get(row, "CurrencyCode") ?? "USD",
                        PaymentTermsDays = paymentTermsDays,
                        CreditLimit = creditLimit,
                        Rating = Get(row, "Rating"),
                        Tags = Get(row, "Tags"),
                        AcquisitionSource = acquisitionSource,
                        HealthStatus = healthStatus,
                        IsActive = isActive,
                        CreatedAtUtc = DateTime.UtcNow,
                    };

                    await _customerRepository.AddAsync(customer);
                    await _auditLogService.LogAsync(EntityType, customer.Id, "Created", $"Customer {customer.CustomerNumber} created via CSV import.", currentUserId);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowErrorDto { RowNumber = rowNumber, Message = ex.Message });
                    result.FailureCount++;
                }
            }

            return result;
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
                PreferredContactMethod = c.PreferredContactMethod,
                DateOfBirth = c.DateOfBirth,
                AnniversaryDate = c.AnniversaryDate,
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
            Tags = customer.Tags,
            HealthStatus = customer.HealthStatus,
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
            Tags = customer.Tags,
            AcquisitionSource = customer.AcquisitionSource,
            HealthStatus = customer.HealthStatus,
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
                PreferredContactMethod = c.PreferredContactMethod,
                DateOfBirth = c.DateOfBirth,
                AnniversaryDate = c.AnniversaryDate,
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
