using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Quotations;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IQuotationService"/>
    public class QuotationService : IQuotationService
    {
        private const string EntityType = "Quotation";

        private static readonly HashSet<QuotationStatus> TerminalStatuses =
            [QuotationStatus.Accepted, QuotationStatus.Rejected, QuotationStatus.Expired];

        /// <summary>Allowed forward transitions — Draft must be Sent before it can be Accepted/Rejected/Expired.</summary>
        private static readonly Dictionary<QuotationStatus, HashSet<QuotationStatus>> AllowedTransitions = new()
        {
            [QuotationStatus.Draft] = [QuotationStatus.Sent],
            [QuotationStatus.Sent] = [QuotationStatus.Accepted, QuotationStatus.Rejected, QuotationStatus.Expired],
        };

        private readonly IQuotationRepository _quotationRepository;
        private readonly IOpportunityRepository _opportunityRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public QuotationService(
            IQuotationRepository quotationRepository,
            IOpportunityRepository opportunityRepository,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _quotationRepository = quotationRepository;
            _opportunityRepository = opportunityRepository;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<QuotationListItemDto>> SearchAsync(
            string? search, QuotationStatus? status, Guid? opportunityId, Guid? customerId, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _quotationRepository.SearchAsync(search, status, opportunityId, customerId, page, pageSize, sortBy, sortDescending);

            return new PagedResult<QuotationListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<QuotationDto>> GetByIdAsync(Guid id)
        {
            var quotation = await _quotationRepository.GetByIdAsync(id);
            if (quotation is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotation not found.");
            }

            var hasSalesOrder = await _quotationRepository.HasSalesOrderAsync(id);
            return ApiResponse<QuotationDto>.SuccessResponse(Map(quotation, hasSalesOrder));
        }

        public async Task<ApiResponse<QuotationDto>> CreateAsync(CreateQuotationRequest request, Guid? currentUserId)
        {
            var opportunity = await _opportunityRepository.GetByIdAsync(request.OpportunityId);
            if (opportunity is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("The selected opportunity does not exist.");
            }

            if (opportunity.Stage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotations cannot be created for a closed opportunity.");
            }

            var lineItems = BuildLineItems(request.LineItems);
            var (subtotal, taxTotal, grandTotal) = SumTotals(lineItems);

            var quotation = new Quotation
            {
                QuotationNumber = await _quotationRepository.GetNextQuotationNumberAsync(),
                Version = 1,
                OpportunityId = opportunity.Id,
                CustomerId = opportunity.CustomerId,
                Status = QuotationStatus.Draft,
                ValidUntil = request.ValidUntil,
                TermsAndConditions = request.TermsAndConditions,
                Notes = request.Notes,
                Subtotal = subtotal,
                TaxTotal = taxTotal,
                GrandTotal = grandTotal,
                AssignedToUserId = request.AssignedToUserId ?? opportunity.AssignedToUserId,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _quotationRepository.AddAsync(quotation);
            await _quotationRepository.ReplaceLineItemsAsync(quotation.Id, lineItems);
            await _auditLogService.LogAsync(EntityType, quotation.Id, "Created", $"Quotation {quotation.QuotationNumber} created.", currentUserId);

            var created = await _quotationRepository.GetByIdAsync(quotation.Id);
            return ApiResponse<QuotationDto>.SuccessResponse(Map(created!, false), "Quotation created.");
        }

        public async Task<ApiResponse<QuotationDto>> UpdateAsync(Guid id, UpdateQuotationRequest request, Guid? currentUserId)
        {
            var quotation = await _quotationRepository.GetByIdAsync(id);
            if (quotation is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotation not found.");
            }

            if (quotation.Status != QuotationStatus.Draft)
            {
                return ApiResponse<QuotationDto>.FailureResponse(
                    "This quotation has already been sent and can no longer be edited directly — create a new version instead.");
            }

            var opportunity = await _opportunityRepository.GetByIdAsync(quotation.OpportunityId);
            if (opportunity is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("The related opportunity no longer exists.");
            }

            if (opportunity.Stage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotations cannot be edited for a closed opportunity.");
            }

            var lineItems = BuildLineItems(request.LineItems);
            var (subtotal, taxTotal, grandTotal) = SumTotals(lineItems);

            quotation.ValidUntil = request.ValidUntil;
            quotation.TermsAndConditions = request.TermsAndConditions;
            quotation.Notes = request.Notes;
            quotation.Subtotal = subtotal;
            quotation.TaxTotal = taxTotal;
            quotation.GrandTotal = grandTotal;
            quotation.UpdatedAtUtc = DateTime.UtcNow;

            await _quotationRepository.UpdateAsync(quotation);
            await _quotationRepository.ReplaceLineItemsAsync(id, lineItems);
            await _auditLogService.LogAsync(EntityType, id, "Updated", "Quotation details updated.", currentUserId);

            var updated = await _quotationRepository.GetByIdAsync(id);
            return ApiResponse<QuotationDto>.SuccessResponse(Map(updated!, false), "Quotation updated.");
        }

        public async Task<ApiResponse<QuotationDto>> UpdateStatusAsync(Guid id, UpdateQuotationStatusRequest request, Guid? currentUserId)
        {
            var quotation = await _quotationRepository.GetByIdAsync(id);
            if (quotation is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotation not found.");
            }

            if (!AllowedTransitions.TryGetValue(quotation.Status, out var allowed) || !allowed.Contains(request.Status))
            {
                return ApiResponse<QuotationDto>.FailureResponse($"Cannot move a quotation from {quotation.Status} to {request.Status}.");
            }

            var oldStatus = quotation.Status;
            quotation.Status = request.Status;
            quotation.UpdatedAtUtc = DateTime.UtcNow;

            await _quotationRepository.UpdateAsync(quotation);
            await _auditLogService.LogAsync(EntityType, id, "StatusChanged", $"Status changed from {oldStatus} to {request.Status}.", currentUserId);

            if (request.Status == QuotationStatus.Accepted)
            {
                var recipients = new HashSet<Guid>();
                if (quotation.AssignedToUserId is not null) recipients.Add(quotation.AssignedToUserId.Value);
                if (quotation.CreatedByUserId is not null) recipients.Add(quotation.CreatedByUserId.Value);

                foreach (var recipient in recipients)
                {
                    await _notificationService.NotifyAsync(
                        recipient,
                        $"Quotation {quotation.QuotationNumber} was accepted — ready to convert to a sales order.",
                        RelatedEntityType.Quotation,
                        quotation.Id);
                }
            }

            var updated = await _quotationRepository.GetByIdAsync(id);
            var hasSalesOrder = await _quotationRepository.HasSalesOrderAsync(id);
            return ApiResponse<QuotationDto>.SuccessResponse(Map(updated!, hasSalesOrder), "Quotation status updated.");
        }

        public async Task<ApiResponse<QuotationDto>> AssignAsync(Guid id, AssignQuotationRequest request, Guid? currentUserId)
        {
            var quotation = await _quotationRepository.GetByIdAsync(id);
            if (quotation is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotation not found.");
            }

            quotation.AssignedToUserId = request.UserId;
            quotation.UpdatedAtUtc = DateTime.UtcNow;

            await _quotationRepository.UpdateAsync(quotation);
            await _auditLogService.LogAsync(EntityType, id, "Assigned", $"Quotation {quotation.QuotationNumber} assigned.", currentUserId);

            var updated = await _quotationRepository.GetByIdAsync(id);
            await _notificationService.NotifyAsync(
                request.UserId,
                $"You were assigned quotation {updated!.QuotationNumber}.",
                RelatedEntityType.Quotation,
                updated.Id);

            var hasSalesOrder = await _quotationRepository.HasSalesOrderAsync(id);
            return ApiResponse<QuotationDto>.SuccessResponse(Map(updated, hasSalesOrder), "Quotation assigned.");
        }

        public async Task<ApiResponse<QuotationDto>> CreateNewVersionAsync(Guid id, Guid? currentUserId)
        {
            var source = await _quotationRepository.GetByIdAsync(id);
            if (source is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotation not found.");
            }

            var opportunity = await _opportunityRepository.GetByIdAsync(source.OpportunityId);
            if (opportunity is null)
            {
                return ApiResponse<QuotationDto>.FailureResponse("The related opportunity no longer exists.");
            }

            if (opportunity.Stage is OpportunityStage.ClosedWon or OpportunityStage.ClosedLost)
            {
                return ApiResponse<QuotationDto>.FailureResponse("Quotations cannot be recreated for a closed opportunity.");
            }

            var clonedLineItems = source.LineItems.Select(li => new QuotationLineItem
            {
                ProductName = li.ProductName,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
                TaxPercent = li.TaxPercent,
            }).ToList();

            var (subtotal, taxTotal, grandTotal) = SumTotals(clonedLineItems);

            var newVersion = new Quotation
            {
                QuotationNumber = source.QuotationNumber,
                Version = source.Version + 1,
                OpportunityId = source.OpportunityId,
                CustomerId = source.CustomerId,
                Status = QuotationStatus.Draft,
                ValidUntil = source.ValidUntil,
                TermsAndConditions = source.TermsAndConditions,
                Notes = source.Notes,
                Subtotal = subtotal,
                TaxTotal = taxTotal,
                GrandTotal = grandTotal,
                AssignedToUserId = source.AssignedToUserId,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _quotationRepository.AddAsync(newVersion);
            await _quotationRepository.ReplaceLineItemsAsync(newVersion.Id, clonedLineItems);
            await _auditLogService.LogAsync(
                EntityType, newVersion.Id, "Created",
                $"Version {newVersion.Version} of quotation {newVersion.QuotationNumber} created (from version {source.Version}).",
                currentUserId);

            var created = await _quotationRepository.GetByIdAsync(newVersion.Id);
            return ApiResponse<QuotationDto>.SuccessResponse(Map(created!, false), "New quotation version created.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId)
        {
            var quotation = await _quotationRepository.GetByIdAsync(id);
            if (quotation is null)
            {
                return ApiResponse<bool>.FailureResponse("Quotation not found.");
            }

            if (quotation.Status != QuotationStatus.Draft)
            {
                return ApiResponse<bool>.FailureResponse("Only a Draft quotation can be deleted — reject or let a sent quotation expire instead.");
            }

            await _quotationRepository.DeleteAsync(quotation);
            await _auditLogService.LogAsync(EntityType, id, "Deleted", $"Quotation {quotation.QuotationNumber} deleted.", currentUserId);
            return ApiResponse<bool>.SuccessResponse(true, "Quotation deleted.");
        }

        private static List<QuotationLineItem> BuildLineItems(IEnumerable<SaveQuotationLineItemRequest> requests)
            => requests.Select(li => new QuotationLineItem
            {
                ProductName = li.ProductName.Trim(),
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
                TaxPercent = li.TaxPercent,
            }).ToList();

        private static (decimal Subtotal, decimal TaxTotal, decimal GrandTotal) SumTotals(IEnumerable<QuotationLineItem> lineItems)
        {
            var items = lineItems.ToList();
            return (items.Sum(li => li.SubtotalAmount), items.Sum(li => li.TaxAmount), items.Sum(li => li.LineTotal));
        }

        private static QuotationListItemDto MapListItem(Quotation quotation) => new()
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            Version = quotation.Version,
            OpportunityId = quotation.OpportunityId,
            OpportunityName = quotation.Opportunity?.Name ?? string.Empty,
            CustomerId = quotation.CustomerId,
            CustomerName = quotation.Customer?.DisplayName ?? string.Empty,
            Status = quotation.Status,
            GrandTotal = quotation.GrandTotal,
            ValidUntil = quotation.ValidUntil,
            AssignedToUserId = quotation.AssignedToUserId,
            AssignedToUserName = quotation.AssignedToUser?.FullName,
            CreatedAtUtc = quotation.CreatedAtUtc,
        };

        private static QuotationDto Map(Quotation quotation, bool hasSalesOrder) => new()
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            Version = quotation.Version,
            OpportunityId = quotation.OpportunityId,
            OpportunityName = quotation.Opportunity?.Name ?? string.Empty,
            CustomerId = quotation.CustomerId,
            CustomerName = quotation.Customer?.DisplayName ?? string.Empty,
            Status = quotation.Status,
            ValidUntil = quotation.ValidUntil,
            TermsAndConditions = quotation.TermsAndConditions,
            Notes = quotation.Notes,
            Subtotal = quotation.Subtotal,
            TaxTotal = quotation.TaxTotal,
            GrandTotal = quotation.GrandTotal,
            AssignedToUserId = quotation.AssignedToUserId,
            AssignedToUserName = quotation.AssignedToUser?.FullName,
            CreatedAtUtc = quotation.CreatedAtUtc,
            UpdatedAtUtc = quotation.UpdatedAtUtc,
            HasSalesOrder = hasSalesOrder,
            LineItems = quotation.LineItems.Select(li => new QuotationLineItemDto
            {
                Id = li.Id,
                ProductName = li.ProductName,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
                TaxPercent = li.TaxPercent,
                LineTotal = li.LineTotal,
            }).ToList(),
        };
    }
}
