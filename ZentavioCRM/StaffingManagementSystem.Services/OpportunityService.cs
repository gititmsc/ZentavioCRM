using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Opportunities;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IOpportunityService"/>
    public class OpportunityService : IOpportunityService
    {
        private const string EntityType = "Opportunity";

        private static readonly HashSet<OpportunityStage> TerminalStages = [OpportunityStage.ClosedWon, OpportunityStage.ClosedLost];

        private readonly IOpportunityRepository _opportunityRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IAccessScopeService _accessScopeService;

        public OpportunityService(
            IOpportunityRepository opportunityRepository,
            ICustomerRepository customerRepository,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IAccessScopeService accessScopeService)
        {
            _opportunityRepository = opportunityRepository;
            _customerRepository = customerRepository;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _accessScopeService = accessScopeService;
        }

        /// <summary>In-memory record-visibility check for a single already-fetched Opportunity. Returns true (no restriction) when currentUserId is null, since that only happens for internal/system callers, never an authenticated HTTP request.</summary>
        private async Task<bool> CanAccessAsync(Guid? currentUserId, Opportunity opportunity)
        {
            if (currentUserId is null)
            {
                return true;
            }

            var scope = await _accessScopeService.GetForUserAsync(currentUserId.Value);
            return scope.CanSee(opportunity.AssignedToUserId, opportunity.CreatedByUserId);
        }

        public async Task<PagedResult<OpportunityListItemDto>> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize, Guid? currentUserId = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            AccessScope? accessScope = currentUserId is null ? null : await _accessScopeService.GetForUserAsync(currentUserId.Value);
            var (items, totalCount) = await _opportunityRepository.SearchAsync(search, stage, customerId, assignedToUserId, page, pageSize, accessScope);

            return new PagedResult<OpportunityListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<OpportunityDto>> GetByIdAsync(Guid id, Guid? currentUserId = null)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (!await CanAccessAsync(currentUserId, opportunity))
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            return ApiResponse<OpportunityDto>.SuccessResponse(Map(opportunity));
        }

        public async Task<ApiResponse<OpportunityDto>> CreateAsync(SaveOpportunityRequest request, Guid? currentUserId)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("The selected customer does not exist.");
            }

            var contactValidationError = ValidateContacts(request, customer);
            if (contactValidationError is not null)
            {
                return contactValidationError;
            }

            var opportunity = new Opportunity
            {
                OpportunityNumber = await _opportunityRepository.GetNextOpportunityNumberAsync(),
                Name = request.Name.Trim(),
                CustomerId = request.CustomerId,
                Value = ResolveValue(request),
                CurrencyCode = ResolveCurrencyCode(request.CurrencyCode, customer.CurrencyCode),
                Probability = request.Probability,
                Products = request.Products,
                Competitors = request.Competitors,
                ExpectedCloseDate = request.ExpectedCloseDate,
                AssignedToUserId = request.AssignedToUserId,
                Stage = OpportunityStage.Qualification,
                Notes = request.Notes,
                NextStep = request.NextStep,
                NextStepDate = request.NextStepDate,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _opportunityRepository.AddAsync(opportunity);
            await ReplaceLineItemsAsync(opportunity.Id, request);
            await ReplaceContactsAsync(opportunity.Id, request);
            await _auditLogService.LogAsync(EntityType, opportunity.Id, "Created", $"Opportunity {opportunity.OpportunityNumber} created.", currentUserId);

            var created = await _opportunityRepository.GetByIdAsync(opportunity.Id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(created!), "Opportunity created.");
        }

        public async Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, SaveOpportunityRequest request, Guid? currentUserId)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (!await CanAccessAsync(currentUserId, opportunity))
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (TerminalStages.Contains(opportunity.Stage))
            {
                return ApiResponse<OpportunityDto>.FailureResponse(
                    $"This opportunity is {opportunity.Stage} and can no longer be edited.");
            }

            // Always re-fetched (even if CustomerId is unchanged) since it's needed both to validate the
            // buying-committee contacts below and to resolve the default CurrencyCode.
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("The selected customer does not exist.");
            }

            var contactValidationError = ValidateContacts(request, customer);
            if (contactValidationError is not null)
            {
                return contactValidationError;
            }

            opportunity.Name = request.Name.Trim();
            opportunity.CustomerId = request.CustomerId;
            opportunity.Value = ResolveValue(request);
            opportunity.CurrencyCode = ResolveCurrencyCode(request.CurrencyCode, customer.CurrencyCode);
            opportunity.Probability = request.Probability;
            opportunity.Products = request.Products;
            opportunity.Competitors = request.Competitors;
            opportunity.ExpectedCloseDate = request.ExpectedCloseDate;
            opportunity.Notes = request.Notes;
            opportunity.NextStep = request.NextStep;
            opportunity.NextStepDate = request.NextStepDate;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;

            // AssignedToUserId is changed exclusively through AssignAsync, matching the Lead convention.
            await _opportunityRepository.UpdateAsync(opportunity);
            await ReplaceLineItemsAsync(id, request);
            await ReplaceContactsAsync(id, request);
            await _auditLogService.LogAsync(EntityType, id, "Updated", "Opportunity details updated.", currentUserId);

            var updated = await _opportunityRepository.GetByIdAsync(id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(updated!), "Opportunity updated.");
        }

        public async Task<ApiResponse<OpportunityDto>> UpdateStageAsync(Guid id, UpdateOpportunityStageRequest request, Guid? currentUserId)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (!await CanAccessAsync(currentUserId, opportunity))
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

            var oldStage = opportunity.Stage;
            opportunity.Stage = request.Stage;
            opportunity.LostReason = request.Stage == OpportunityStage.ClosedLost ? request.Reason : null;
            opportunity.ClosedAtUtc = TerminalStages.Contains(request.Stage) ? DateTime.UtcNow : null;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;

            await _opportunityRepository.UpdateAsync(opportunity);
            await _auditLogService.LogAsync(EntityType, id, "StageChanged", $"Stage changed from {oldStage} to {request.Stage}.", currentUserId);

            if (TerminalStages.Contains(request.Stage))
            {
                var recipients = new HashSet<Guid>();
                if (opportunity.AssignedToUserId is not null) recipients.Add(opportunity.AssignedToUserId.Value);
                if (opportunity.CreatedByUserId is not null) recipients.Add(opportunity.CreatedByUserId.Value);

                foreach (var recipient in recipients)
                {
                    await _notificationService.NotifyAsync(
                        recipient,
                        $"Opportunity {opportunity.OpportunityNumber} — {opportunity.Name} was marked {request.Stage}.",
                        RelatedEntityType.Opportunity,
                        opportunity.Id);
                }
            }

            var updated = await _opportunityRepository.GetByIdAsync(id);
            return ApiResponse<OpportunityDto>.SuccessResponse(Map(updated!), "Opportunity stage updated.");
        }

        public async Task<ApiResponse<OpportunityDto>> AssignAsync(Guid id, AssignOpportunityRequest request, Guid? currentUserId)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<OpportunityDto>.FailureResponse("Opportunity not found.");
            }

            if (!await CanAccessAsync(currentUserId, opportunity))
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
            await _auditLogService.LogAsync(EntityType, id, "Assigned", $"Opportunity {opportunity.OpportunityNumber} assigned.", currentUserId);

            var updated = await _opportunityRepository.GetByIdAsync(id);
            await _notificationService.NotifyAsync(
                request.UserId,
                $"You were assigned opportunity {updated!.OpportunityNumber} — {updated.Name}.",
                RelatedEntityType.Opportunity,
                updated.Id);

            return ApiResponse<OpportunityDto>.SuccessResponse(Map(updated), "Opportunity assigned.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(id);
            if (opportunity is null)
            {
                return ApiResponse<bool>.FailureResponse("Opportunity not found.");
            }

            if (!await CanAccessAsync(currentUserId, opportunity))
            {
                return ApiResponse<bool>.FailureResponse("Opportunity not found.");
            }

            await _opportunityRepository.DeleteAsync(opportunity);
            await _auditLogService.LogAsync(EntityType, id, "Deleted", $"Opportunity {opportunity.OpportunityNumber} deleted.", currentUserId);
            return ApiResponse<bool>.SuccessResponse(true, "Opportunity deleted.");
        }

        /// <summary>When the request includes line items, the opportunity's Value is always the computed sum of those lines — the submitted Value is ignored in that case so the two can never drift apart.</summary>
        private static decimal? ResolveValue(SaveOpportunityRequest request)
            => request.LineItems.Count > 0
                ? request.LineItems.Sum(li => LineTotal(li.Quantity, li.UnitPrice, li.DiscountPercent))
                : request.Value;

        private static decimal LineTotal(decimal quantity, decimal unitPrice, decimal? discountPercent)
            => Math.Round(quantity * unitPrice * (1 - (discountPercent ?? 0) / 100m), 2);

        /// <summary>Falls back to the customer's currency when the request doesn't specify one — preserves the old implicit behavior as a default while making it explicit and overridable.</summary>
        private static string ResolveCurrencyCode(string? requestedCurrencyCode, string customerCurrencyCode)
            => string.IsNullOrWhiteSpace(requestedCurrencyCode) ? customerCurrencyCode : requestedCurrencyCode.Trim().ToUpperInvariant();

        /// <summary>Every ContactPersonId on the request must belong to the selected Customer — a buying-committee member can't be a contact from a different account.</summary>
        private static ApiResponse<OpportunityDto>? ValidateContacts(SaveOpportunityRequest request, Customer customer)
        {
            if (request.Contacts.Count == 0)
            {
                return null;
            }

            var validContactIds = customer.Contacts.Select(c => c.Id).ToHashSet();
            var hasInvalidContact = request.Contacts.Any(c => !validContactIds.Contains(c.ContactPersonId));

            return hasInvalidContact
                ? ApiResponse<OpportunityDto>.FailureResponse("One or more selected buying-committee contacts do not belong to this opportunity's customer.")
                : null;
        }

        private async Task ReplaceLineItemsAsync(Guid opportunityId, SaveOpportunityRequest request)
        {
            var lineItems = request.LineItems.Select(li => new OpportunityLineItem
            {
                ProductName = li.ProductName.Trim(),
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
            });

            await _opportunityRepository.ReplaceLineItemsAsync(opportunityId, lineItems);
        }

        private async Task ReplaceContactsAsync(Guid opportunityId, SaveOpportunityRequest request)
        {
            var contacts = request.Contacts.Select(c => new OpportunityContact
            {
                ContactPersonId = c.ContactPersonId,
                Role = c.Role,
                Notes = c.Notes,
            });

            await _opportunityRepository.ReplaceContactsAsync(opportunityId, contacts);
        }

        private static OpportunityListItemDto MapListItem(Opportunity opportunity) => new()
        {
            Id = opportunity.Id,
            OpportunityNumber = opportunity.OpportunityNumber,
            Name = opportunity.Name,
            CustomerId = opportunity.CustomerId,
            CustomerName = opportunity.Customer?.DisplayName ?? string.Empty,
            Value = opportunity.Value,
            CurrencyCode = opportunity.CurrencyCode,
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
            CurrencyCode = opportunity.CurrencyCode,
            Probability = opportunity.Probability,
            Products = opportunity.Products,
            Competitors = opportunity.Competitors,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Stage = opportunity.Stage,
            AssignedToUserId = opportunity.AssignedToUserId,
            AssignedToUserName = opportunity.AssignedToUser?.FullName,
            SourceLeadId = opportunity.SourceLeadId,
            Notes = opportunity.Notes,
            NextStep = opportunity.NextStep,
            NextStepDate = opportunity.NextStepDate,
            LostReason = opportunity.LostReason,
            ClosedAtUtc = opportunity.ClosedAtUtc,
            CreatedAtUtc = opportunity.CreatedAtUtc,
            UpdatedAtUtc = opportunity.UpdatedAtUtc,
            LineItems = opportunity.LineItems.Select(li => new OpportunityLineItemDto
            {
                Id = li.Id,
                ProductName = li.ProductName,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
                LineTotal = li.LineTotal,
            }).ToList(),
            Contacts = opportunity.Contacts.Select(oc => new OpportunityContactDto
            {
                Id = oc.Id,
                ContactPersonId = oc.ContactPersonId,
                ContactPersonName = oc.ContactPerson?.FullName ?? string.Empty,
                ContactPersonDesignation = oc.ContactPerson?.Designation,
                Role = oc.Role,
                Notes = oc.Notes,
            }).ToList(),
        };
    }
}
