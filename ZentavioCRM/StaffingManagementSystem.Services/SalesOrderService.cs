using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.SalesOrders;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ISalesOrderService"/>
    public class SalesOrderService : ISalesOrderService
    {
        private const string EntityType = "SalesOrder";

        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IQuotationRepository _quotationRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository,
            IQuotationRepository quotationRepository,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _salesOrderRepository = salesOrderRepository;
            _quotationRepository = quotationRepository;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<SalesOrderListItemDto>> SearchAsync(
            string? search, SalesOrderStatus? status, Guid? customerId, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _salesOrderRepository.SearchAsync(search, status, customerId, page, pageSize);

            return new PagedResult<SalesOrderListItemDto>
            {
                Items = items.Select(MapListItem).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<SalesOrderDto>> GetByIdAsync(Guid id)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            return salesOrder is null
                ? ApiResponse<SalesOrderDto>.FailureResponse("Sales order not found.")
                : ApiResponse<SalesOrderDto>.SuccessResponse(Map(salesOrder));
        }

        public async Task<ApiResponse<SalesOrderDto>> ConvertFromQuotationAsync(ConvertQuotationToSalesOrderRequest request, Guid? currentUserId)
        {
            var quotation = await _quotationRepository.GetByIdAsync(request.QuotationId);
            if (quotation is null)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("Quotation not found.");
            }

            if (quotation.Status != QuotationStatus.Accepted)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("Only an Accepted quotation can be converted to a sales order.");
            }

            if (await _quotationRepository.HasSalesOrderAsync(quotation.Id))
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("This quotation has already been converted to a sales order.");
            }

            var lineItems = quotation.LineItems.Select(li => new SalesOrderLineItem
            {
                ProductName = li.ProductName,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
                TaxPercent = li.TaxPercent,
                DeliveredQuantity = 0,
            }).ToList();

            var salesOrder = new SalesOrder
            {
                SalesOrderNumber = await _salesOrderRepository.GetNextSalesOrderNumberAsync(),
                QuotationId = quotation.Id,
                CustomerId = quotation.CustomerId,
                Status = SalesOrderStatus.Confirmed,
                OrderDate = DateTime.UtcNow,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                Notes = request.Notes,
                Subtotal = quotation.Subtotal,
                TaxTotal = quotation.TaxTotal,
                GrandTotal = quotation.GrandTotal,
                AssignedToUserId = request.AssignedToUserId ?? quotation.AssignedToUserId,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
                LineItems = lineItems,
            };

            await _salesOrderRepository.AddAsync(salesOrder);
            await _auditLogService.LogAsync(
                EntityType, salesOrder.Id, "Created",
                $"Sales order {salesOrder.SalesOrderNumber} created from quotation {quotation.QuotationNumber}.",
                currentUserId);
            await _auditLogService.LogAsync(
                "Quotation", quotation.Id, "Converted",
                $"Converted to sales order {salesOrder.SalesOrderNumber}.",
                currentUserId);

            var created = await _salesOrderRepository.GetByIdAsync(salesOrder.Id);
            return ApiResponse<SalesOrderDto>.SuccessResponse(Map(created!), "Sales order created.");
        }

        public async Task<ApiResponse<SalesOrderDto>> UpdateAsync(Guid id, UpdateSalesOrderRequest request, Guid? currentUserId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            if (salesOrder is null)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("Sales order not found.");
            }

            if (salesOrder.Status == SalesOrderStatus.Cancelled)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("This sales order has been cancelled and can no longer be edited.");
            }

            salesOrder.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
            salesOrder.Notes = request.Notes;
            salesOrder.UpdatedAtUtc = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(salesOrder);
            await _auditLogService.LogAsync(EntityType, id, "Updated", "Sales order details updated.", currentUserId);

            var updated = await _salesOrderRepository.GetByIdAsync(id);
            return ApiResponse<SalesOrderDto>.SuccessResponse(Map(updated!), "Sales order updated.");
        }

        public async Task<ApiResponse<SalesOrderDto>> AssignAsync(Guid id, AssignSalesOrderRequest request, Guid? currentUserId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            if (salesOrder is null)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("Sales order not found.");
            }

            salesOrder.AssignedToUserId = request.UserId;
            salesOrder.UpdatedAtUtc = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(salesOrder);
            await _auditLogService.LogAsync(EntityType, id, "Assigned", $"Sales order {salesOrder.SalesOrderNumber} assigned.", currentUserId);

            var updated = await _salesOrderRepository.GetByIdAsync(id);
            await _notificationService.NotifyAsync(
                request.UserId,
                $"You were assigned sales order {updated!.SalesOrderNumber}.",
                RelatedEntityType.SalesOrder,
                updated.Id);

            return ApiResponse<SalesOrderDto>.SuccessResponse(Map(updated), "Sales order assigned.");
        }

        public async Task<ApiResponse<SalesOrderDto>> RecordDeliveryAsync(Guid id, RecordDeliveryRequest request, Guid? currentUserId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            if (salesOrder is null)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("Sales order not found.");
            }

            if (salesOrder.Status is SalesOrderStatus.Cancelled or SalesOrderStatus.Delivered)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse($"This sales order is {salesOrder.Status} and cannot receive further deliveries.");
            }

            var lineItemsById = salesOrder.LineItems.ToDictionary(li => li.Id);

            foreach (var line in request.Lines)
            {
                if (!lineItemsById.TryGetValue(line.LineItemId, out var lineItem))
                {
                    return ApiResponse<SalesOrderDto>.FailureResponse("One or more line items do not belong to this sales order.");
                }

                var newDelivered = lineItem.DeliveredQuantity + line.DeliveredQuantity;
                if (newDelivered > lineItem.Quantity)
                {
                    return ApiResponse<SalesOrderDto>.FailureResponse(
                        $"Cannot deliver {line.DeliveredQuantity} of \"{lineItem.ProductName}\" — only {lineItem.Quantity - lineItem.DeliveredQuantity} remains undelivered.");
                }

                lineItem.DeliveredQuantity = newDelivered;
            }

            await _salesOrderRepository.SaveLineItemsAsync(salesOrder.LineItems);

            var allDelivered = salesOrder.LineItems.All(li => li.DeliveredQuantity >= li.Quantity);
            var anyDelivered = salesOrder.LineItems.Any(li => li.DeliveredQuantity > 0);
            salesOrder.Status = allDelivered ? SalesOrderStatus.Delivered : anyDelivered ? SalesOrderStatus.PartiallyDelivered : salesOrder.Status;
            salesOrder.UpdatedAtUtc = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(salesOrder);
            await _auditLogService.LogAsync(
                EntityType, id, "DeliveryRecorded",
                $"Delivery recorded — order is now {salesOrder.Status}.",
                currentUserId);

            if (salesOrder.Status == SalesOrderStatus.Delivered)
            {
                var recipients = new HashSet<Guid>();
                if (salesOrder.AssignedToUserId is not null) recipients.Add(salesOrder.AssignedToUserId.Value);
                if (salesOrder.CreatedByUserId is not null) recipients.Add(salesOrder.CreatedByUserId.Value);

                foreach (var recipient in recipients)
                {
                    await _notificationService.NotifyAsync(
                        recipient,
                        $"Sales order {salesOrder.SalesOrderNumber} has been fully delivered.",
                        RelatedEntityType.SalesOrder,
                        salesOrder.Id);
                }
            }

            var updated = await _salesOrderRepository.GetByIdAsync(id);
            return ApiResponse<SalesOrderDto>.SuccessResponse(Map(updated!), "Delivery recorded.");
        }

        public async Task<ApiResponse<SalesOrderDto>> CancelAsync(Guid id, Guid? currentUserId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            if (salesOrder is null)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("Sales order not found.");
            }

            if (salesOrder.Status == SalesOrderStatus.Delivered)
            {
                return ApiResponse<SalesOrderDto>.FailureResponse("A fully delivered sales order cannot be cancelled.");
            }

            salesOrder.Status = SalesOrderStatus.Cancelled;
            salesOrder.UpdatedAtUtc = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(salesOrder);
            await _auditLogService.LogAsync(EntityType, id, "Cancelled", $"Sales order {salesOrder.SalesOrderNumber} cancelled.", currentUserId);

            var updated = await _salesOrderRepository.GetByIdAsync(id);
            return ApiResponse<SalesOrderDto>.SuccessResponse(Map(updated!), "Sales order cancelled.");
        }

        private static SalesOrderListItemDto MapListItem(SalesOrder salesOrder) => new()
        {
            Id = salesOrder.Id,
            SalesOrderNumber = salesOrder.SalesOrderNumber,
            QuotationId = salesOrder.QuotationId,
            QuotationNumber = salesOrder.Quotation?.QuotationNumber ?? string.Empty,
            CustomerId = salesOrder.CustomerId,
            CustomerName = salesOrder.Customer?.DisplayName ?? string.Empty,
            Status = salesOrder.Status,
            GrandTotal = salesOrder.GrandTotal,
            OrderDate = salesOrder.OrderDate,
            ExpectedDeliveryDate = salesOrder.ExpectedDeliveryDate,
            AssignedToUserId = salesOrder.AssignedToUserId,
            AssignedToUserName = salesOrder.AssignedToUser?.FullName,
        };

        private static SalesOrderDto Map(SalesOrder salesOrder) => new()
        {
            Id = salesOrder.Id,
            SalesOrderNumber = salesOrder.SalesOrderNumber,
            QuotationId = salesOrder.QuotationId,
            QuotationNumber = salesOrder.Quotation?.QuotationNumber ?? string.Empty,
            CustomerId = salesOrder.CustomerId,
            CustomerName = salesOrder.Customer?.DisplayName ?? string.Empty,
            Status = salesOrder.Status,
            OrderDate = salesOrder.OrderDate,
            ExpectedDeliveryDate = salesOrder.ExpectedDeliveryDate,
            Notes = salesOrder.Notes,
            Subtotal = salesOrder.Subtotal,
            TaxTotal = salesOrder.TaxTotal,
            GrandTotal = salesOrder.GrandTotal,
            AssignedToUserId = salesOrder.AssignedToUserId,
            AssignedToUserName = salesOrder.AssignedToUser?.FullName,
            CreatedAtUtc = salesOrder.CreatedAtUtc,
            UpdatedAtUtc = salesOrder.UpdatedAtUtc,
            LineItems = salesOrder.LineItems.Select(li => new SalesOrderLineItemDto
            {
                Id = li.Id,
                ProductName = li.ProductName,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                DiscountPercent = li.DiscountPercent,
                TaxPercent = li.TaxPercent,
                DeliveredQuantity = li.DeliveredQuantity,
                LineTotal = li.LineTotal,
            }).ToList(),
        };
    }
}
