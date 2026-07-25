using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ILeadService"/>
    public class LeadService : ILeadService
    {
        private static readonly HashSet<LeadStatus> TerminalStatuses = [LeadStatus.Converted, LeadStatus.Lost, LeadStatus.Junk];

        private readonly ILeadRepository _leadRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IOpportunityRepository _opportunityRepository;

        public LeadService(
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            IOpportunityRepository opportunityRepository)
        {
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _opportunityRepository = opportunityRepository;
        }

        public async Task<PagedResult<LeadListItemDto>> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _leadRepository.SearchAsync(search, status, assignedToUserId, page, pageSize);

            return new PagedResult<LeadListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<LeadDto>> GetByIdAsync(Guid id)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            return lead is null
                ? ApiResponse<LeadDto>.FailureResponse("Lead not found.")
                : ApiResponse<LeadDto>.SuccessResponse(Map(lead));
        }

        public async Task<ApiResponse<LeadDto>> CreateAsync(SaveLeadRequest request, Guid? currentUserId)
        {
            var lead = new Lead
            {
                LeadNumber = await _leadRepository.GetNextLeadNumberAsync(),
                CompanyName = request.CompanyName.Trim(),
                ContactName = request.ContactName.Trim(),
                Email = request.Email,
                Mobile = request.Mobile,
                Industry = request.Industry,
                Source = request.Source,
                Campaign = request.Campaign,
                Budget = request.Budget,
                Timeline = request.Timeline,
                ExpectedValue = request.ExpectedValue,
                AssignedToUserId = request.AssignedToUserId,
                Territory = request.Territory,
                Status = request.AssignedToUserId is null ? LeadStatus.New : LeadStatus.Assigned,
                Notes = request.Notes,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _leadRepository.AddAsync(lead);

            var created = await _leadRepository.GetByIdAsync(lead.Id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(created!), "Lead created.");
        }

        public async Task<ApiResponse<LeadDto>> UpdateAsync(Guid id, SaveLeadRequest request)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<LeadDto>.FailureResponse("Lead not found.");
            }

            if (TerminalStatuses.Contains(lead.Status))
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    $"This lead is {lead.Status} and can no longer be edited.",
                    ["Reopen the lead before editing it."]);
            }

            lead.CompanyName = request.CompanyName.Trim();
            lead.ContactName = request.ContactName.Trim();
            lead.Email = request.Email;
            lead.Mobile = request.Mobile;
            lead.Industry = request.Industry;
            lead.Source = request.Source;
            lead.Campaign = request.Campaign;
            lead.Budget = request.Budget;
            lead.Timeline = request.Timeline;
            lead.ExpectedValue = request.ExpectedValue;
            lead.Territory = request.Territory;
            lead.Notes = request.Notes;
            lead.UpdatedAtUtc = DateTime.UtcNow;

            // AssignedToUserId is changed exclusively through AssignAsync, which also drives the status transition.
            await _leadRepository.UpdateAsync(lead);

            var updated = await _leadRepository.GetByIdAsync(id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(updated!), "Lead updated.");
        }

        public async Task<ApiResponse<LeadDto>> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<LeadDto>.FailureResponse("Lead not found.");
            }

            if (lead.Status == LeadStatus.Converted)
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    "A converted lead's status cannot be changed.",
                    ["This lead has already been converted to a customer."]);
            }

            if (request.Status is LeadStatus.Lost or LeadStatus.Junk && string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    "A reason is required when marking a lead as Lost or Junk.",
                    ["Reason is required."]);
            }

            if (request.Status == LeadStatus.Converted)
            {
                return ApiResponse<LeadDto>.FailureResponse(
                    "Use the Convert action to move a lead to Converted — it also creates the customer record.",
                    ["Use POST /api/leads/{id}/convert instead."]);
            }

            lead.Status = request.Status;
            lead.LostReason = request.Status is LeadStatus.Lost or LeadStatus.Junk ? request.Reason : null;
            lead.UpdatedAtUtc = DateTime.UtcNow;

            await _leadRepository.UpdateAsync(lead);

            var updated = await _leadRepository.GetByIdAsync(id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(updated!), "Lead status updated.");
        }

        public async Task<ApiResponse<LeadDto>> AssignAsync(Guid id, AssignLeadRequest request)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<LeadDto>.FailureResponse("Lead not found.");
            }

            if (TerminalStatuses.Contains(lead.Status))
            {
                return ApiResponse<LeadDto>.FailureResponse($"This lead is {lead.Status} and can no longer be reassigned.");
            }

            lead.AssignedToUserId = request.UserId;
            if (lead.Status == LeadStatus.New)
            {
                lead.Status = LeadStatus.Assigned;
            }
            lead.UpdatedAtUtc = DateTime.UtcNow;

            await _leadRepository.UpdateAsync(lead);

            var updated = await _leadRepository.GetByIdAsync(id);
            return ApiResponse<LeadDto>.SuccessResponse(Map(updated!), "Lead assigned.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<bool>.FailureResponse("Lead not found.");
            }

            await _leadRepository.DeleteAsync(lead);
            return ApiResponse<bool>.SuccessResponse(true, "Lead deleted.");
        }

        public async Task<ApiResponse<ConvertLeadResultDto>> ConvertAsync(Guid id, ConvertLeadRequest request)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<ConvertLeadResultDto>.FailureResponse("Lead not found.");
            }

            if (lead.Status == LeadStatus.Converted)
            {
                return ApiResponse<ConvertLeadResultDto>.FailureResponse("This lead has already been converted.");
            }

            if (lead.Status is LeadStatus.Lost or LeadStatus.Junk)
            {
                return ApiResponse<ConvertLeadResultDto>.FailureResponse($"A {lead.Status} lead cannot be converted.");
            }

            var customer = await CreateCustomerFromLeadAsync(lead, request.DisplayName, request.AssignToUserId);

            lead.Status = LeadStatus.Converted;
            lead.ConvertedCustomerId = customer.Id;
            lead.ConvertedAtUtc = DateTime.UtcNow;
            lead.UpdatedAtUtc = DateTime.UtcNow;

            await _leadRepository.UpdateAsync(lead);

            return ApiResponse<ConvertLeadResultDto>.SuccessResponse(
                new ConvertLeadResultDto { CustomerId = customer.Id, CustomerNumber = customer.CustomerNumber },
                "Lead converted to customer.");
        }

        public async Task<ApiResponse<ConvertLeadToOpportunityResultDto>> ConvertToOpportunityAsync(
            Guid id, ConvertLeadToOpportunityRequest request, Guid? currentUserId)
        {
            var lead = await _leadRepository.GetByIdAsync(id);
            if (lead is null)
            {
                return ApiResponse<ConvertLeadToOpportunityResultDto>.FailureResponse("Lead not found.");
            }

            if (lead.Status is LeadStatus.Lost or LeadStatus.Junk)
            {
                return ApiResponse<ConvertLeadToOpportunityResultDto>.FailureResponse($"A {lead.Status} lead cannot be converted.");
            }

            Customer customer;
            if (lead.Status == LeadStatus.Converted && lead.ConvertedCustomerId is not null)
            {
                // Already converted via the plain "Convert to Customer" action — reuse that
                // customer instead of creating a duplicate.
                var existingCustomer = await _customerRepository.GetByIdAsync(lead.ConvertedCustomerId.Value);
                if (existingCustomer is null)
                {
                    return ApiResponse<ConvertLeadToOpportunityResultDto>.FailureResponse(
                        "This lead's linked customer could not be found.");
                }
                customer = existingCustomer;
            }
            else
            {
                customer = await CreateCustomerFromLeadAsync(lead, request.CustomerDisplayName, request.AssignToUserId);

                lead.Status = LeadStatus.Converted;
                lead.ConvertedCustomerId = customer.Id;
                lead.ConvertedAtUtc = DateTime.UtcNow;
                lead.UpdatedAtUtc = DateTime.UtcNow;

                await _leadRepository.UpdateAsync(lead);
            }

            var opportunity = new Opportunity
            {
                OpportunityNumber = await _opportunityRepository.GetNextOpportunityNumberAsync(),
                Name = string.IsNullOrWhiteSpace(request.OpportunityName) ? lead.CompanyName : request.OpportunityName.Trim(),
                CustomerId = customer.Id,
                Value = request.Value ?? lead.ExpectedValue,
                ExpectedCloseDate = request.ExpectedCloseDate,
                AssignedToUserId = request.AssignToUserId ?? lead.AssignedToUserId,
                SourceLeadId = lead.Id,
                Stage = OpportunityStage.Qualification,
                Notes = lead.Notes,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _opportunityRepository.AddAsync(opportunity);

            return ApiResponse<ConvertLeadToOpportunityResultDto>.SuccessResponse(
                new ConvertLeadToOpportunityResultDto
                {
                    CustomerId = customer.Id,
                    CustomerNumber = customer.CustomerNumber,
                    OpportunityId = opportunity.Id,
                    OpportunityNumber = opportunity.OpportunityNumber,
                },
                "Lead converted to opportunity.");
        }

        /// <summary>Builds and persists a Customer from a Lead's own fields — shared by both the plain "convert to customer" and "convert to opportunity" flows.</summary>
        private async Task<Customer> CreateCustomerFromLeadAsync(Lead lead, string? displayNameOverride, Guid? assignToUserIdOverride)
        {
            var displayName = string.IsNullOrWhiteSpace(displayNameOverride) ? lead.CompanyName : displayNameOverride.Trim();
            var assignedToUserId = assignToUserIdOverride ?? lead.AssignedToUserId;

            var customer = new Customer
            {
                CustomerNumber = await _customerRepository.GetNextCustomerNumberAsync(),
                Type = CustomerType.Business,
                LegalName = lead.CompanyName,
                DisplayName = displayName,
                Industry = lead.Industry,
                Email = lead.Email,
                Phone = lead.Mobile,
                AssignedToUserId = assignedToUserId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                Contacts =
                [
                    new ContactPerson
                    {
                        FirstName = lead.ContactName,
                        LastName = string.Empty,
                        Email = lead.Email,
                        Mobile = lead.Mobile,
                        IsPrimary = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    },
                ],
            };

            await _customerRepository.AddAsync(customer);
            return customer;
        }

        private static LeadListItemDto MapListItem(Lead lead) => new()
        {
            Id = lead.Id,
            LeadNumber = lead.LeadNumber,
            CompanyName = lead.CompanyName,
            ContactName = lead.ContactName,
            Source = lead.Source,
            Status = lead.Status,
            ExpectedValue = lead.ExpectedValue,
            AssignedToUserId = lead.AssignedToUserId,
            AssignedToUserName = lead.AssignedToUser?.FullName,
            CreatedAtUtc = lead.CreatedAtUtc,
        };

        private static LeadDto Map(Lead lead) => new()
        {
            Id = lead.Id,
            LeadNumber = lead.LeadNumber,
            CompanyName = lead.CompanyName,
            ContactName = lead.ContactName,
            Email = lead.Email,
            Mobile = lead.Mobile,
            Industry = lead.Industry,
            Source = lead.Source,
            Campaign = lead.Campaign,
            Budget = lead.Budget,
            Timeline = lead.Timeline,
            ExpectedValue = lead.ExpectedValue,
            AssignedToUserId = lead.AssignedToUserId,
            AssignedToUserName = lead.AssignedToUser?.FullName,
            Territory = lead.Territory,
            Status = lead.Status,
            LeadScore = lead.LeadScore,
            AiScore = lead.AiScore,
            Notes = lead.Notes,
            LostReason = lead.LostReason,
            ConvertedCustomerId = lead.ConvertedCustomerId,
            ConvertedAtUtc = lead.ConvertedAtUtc,
            CreatedAtUtc = lead.CreatedAtUtc,
            UpdatedAtUtc = lead.UpdatedAtUtc,
        };
    }
}
