using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Opportunities;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IOpportunityService"/>
    public class OpportunityService : IOpportunityService
    {
        private static readonly HashSet<OpportunityStage> TerminalStages = [OpportunityStage.ClosedWon, OpportunityStage.ClosedLost];

        private readonly IOpportunityRepository _opportunityRepository;
        private readonly ICustomerRepository _customerRepository;

        public OpportunityService(IOpportunityRepository opportunityRepository, ICustomerRepository customerRepository)
        {
            _opportunityRepository = opportunityRepository;
            _customerRepository = customerRepository;
        }

        public async Task<PagedResult<OpportunityListItemDto>> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _opportunityRepository.SearchAsync(search, stage, customerId, assignedToUserId, page, pageSize);

            return new PagedResult<OpportunityListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<OpportunityDto>> GetByIdAsync(Guid id)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            return opportunity is null
                ? ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.")
                : ApiResponse<OpportunityDto>.SuccessResponse(Map(opportunity));
        }

        public async Task<ApiResponse<OpportunityDto>> CreateAsync(SaveOpportunityRequest request, Guid? currentUserId)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("The selected customer does not exist.");
            }

            var opportunity = new Opportunity
            {
                OpportunityNumber = await _opportunityRepository.GetNextOpportunityNumberAsync(),
                Name = request.Name.Trim(),
                CustomerId = request.CustomerId,
                Value = request.Value,
                Probability = request.Probability,
                Products = request.Products,
                Competitors = request.Competitors,
                ExpectedCloseDate = request.ExpectedCloseDate,
                AssignedToUserId = request.AssignedToUserId,
                Stage = OpportunityStage.Qualification,
                Notes = request.Notes,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _opportunityRepository.AddAsync(opportunity);

            var created = await _opportunityRepository.GetByIdAsync(opportunity.Id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(created!), "Opportunity created.");
        }

        public async Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, SaveOpportunityRequest request)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (TerminalStages.Contains(opportunity.Stage))
            {
                return ApiResponse<OpportunityDto>.FailureResponse(
                    $"This opportunity is {opportunity.Stage} and can no longer be edited.");
            }

            if (request.CustomerId != opportunity.CustomerId)
            {
                var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
                if (customer is null)
                {
                    return ApiResponse<OpportunityDto>.FailureResponse("The selected customer does not exist.");
                }
            }

            opportunity.Name = request.Name.Trim();
            opportunity.CustomerId = request.CustomerId;
            opportunity.Value = request.Value;
            opportunity.Probability = request.Probability;
            opportunity.Products = request.Products;
            opportunity.Competitors = request.Competitors;
            opportunity.ExpectedCloseDate = request.ExpectedCloseDate;
            opportunity.Notes = request.Notes;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;

            // AssignedToUserId is changed exclusively through AssignAsync, matching the Lead convention.
            await _opportunityRepository.UpdateAsync(opportunity);

            var updated = await _opportunityRepository.GetByIdAsync(id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(updated!), "Opportunity updated.");
        }

        public async Task<ApiResponse<OpportunityDto>> UpdateStageAsync(Guid id, UpdateOpportunityStageRequest request)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (TerminalStages.Contains(opportunity.Stage))
            {
                return ApiResponse<OpportunityDto>.FailureResponse(
                    $"This opportunity is already {opportunity.Stage} and its stage cannot be changed further.");
            }

            if (request.Stage == OpportunityStage.ClosedLost && string.IsNullOrWhiteSpace(request.Reason))
            {
                return ApiResponse<OpportunityDto>.FailureResponse(
                    "A reason is required when marking an opportunity as Closed Lost.",
                    ["Reason is required."]);
            }

            opportunity.Stage = request.Stage;
            opportunity.LostReason = request.Stage == OpportunityStage.ClosedLost ? request.Reason : null;
            opportunity.ClosedAtUtc = TerminalStages.Contains(request.Stage) ? DateTime.UtcNow : null;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;

            await _opportunityRepository.UpdateAsync(opportunity);

            var updated = await _opportunityRepository.GetByIdAsync(id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(updated!), "Opportunity stage updated.");
        }

        public async Task<ApiResponse<OpportunityDto>> AssignAsync(Guid id, AssignOpportunityRequest request)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (TerminalStages.Contains(opportunity.Stage))
            {
                return ApiResponse<OpportunityDto>.FailureResponse($"This opportunity is {opportunity.Stage} and can no longer be reassigned.");
            }

            opportunity.AssignedToUserId = request.UserId;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;

            await _opportunityRepository.UpdateAsync(opportunity);

            var updated = await _opportunityRepository.GetByIdAsync(id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(updated!), "Opportunity assigned.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<bool>.FailureResponse("Opportunity not found.");
            }

            await _opportunityRepository.DeleteAsync(opportunity);
            return ApiResponse<bool>.SuccessResponse(true, "Opportunity deleted.");
        }

        private static OpportunityListItemDto MapListItem(Opportunity opportunity) => new()
        {
            Id = opportunity.Id,
            OpportunityNumber = opportunity.OpportunityNumber,
            Name = opportunity.Name,
            CustomerId = opportunity.CustomerId,
            CustomerName = opportunity.Customer?.DisplayName ?? string.Empty,
            Value = opportunity.Value,
            Probability = opportunity.Probability,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Stage = opportunity.Stage,
            AssignedToUserId = opportunity.AssignedToUserId,
            AssignedToUserName = opportunity.AssignedToUser?.FullName,
            CreatedAtUtc = opportunity.CreatedAtUtc,
        };

        private static OpportunityDto Map(Opportunity opportunity) => new()
        {
            Id = opportunity.Id,
            OpportunityNumber = opportunity.OpportunityNumber,
            Name = opportunity.Name,
            CustomerId = opportunity.CustomerId,
            CustomerName = opportunity.Customer?.DisplayName ?? string.Empty,
            Value = opportunity.Value,
            Probability = opportunity.Probability,
            Products = opportunity.Products,
            Competitors = opportunity.Competitors,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Stage = opportunity.Stage,
            AssignedToUserId = opportunity.AssignedToUserId,
            AssignedToUserName = opportunity.AssignedToUser?.FullName,
            SourceLeadId = opportunity.SourceLeadId,
            Notes = opportunity.Notes,
            LostReason = opportunity.LostReason,
            ClosedAtUtc = opportunity.ClosedAtUtc,
            CreatedAtUtc = opportunity.CreatedAtUtc,
            UpdatedAtUtc = opportunity.UpdatedAtUtc,
        };
    }
}
