using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Manual, gateway-free billing for a tenant: status/amount/cycle/due-date fields directly on
    /// the tenant row, plus an append-only payment-history ledger (<see cref="Entities.Platform.TenantPayment"/>).
    /// There is no third-party payment processor anywhere behind this interface — every action
    /// here is a platform admin recording what they know happened, not moving money. Lives in the
    /// Infrastructure layer with direct PlatformDbContext access, same reasoning as
    /// <see cref="ITenantProvisioningService"/>.
    /// </summary>
    public interface ITenantBillingService
    {
        /// <summary>Updates whichever billing fields are supplied (PATCH-style — omitted fields
        /// are left unchanged). If <see cref="UpdateTenantBillingRequest.PaymentStatus"/> is set
        /// to Overdue and the tenant is currently Active, this also suspends the tenant via
        /// <see cref="ITenantProvisioningService.SuspendAsync"/> — reversible the same way any
        /// other suspension is. Marking Paid does NOT auto-reactivate a suspended tenant; that
        /// stays a separate, deliberate admin action.</summary>
        Task<ApiResponse<TenantDto>> UpdateBillingAsync(Guid tenantId, UpdateTenantBillingRequest request, Guid? performedByAdminId);

        /// <summary>Records one payment-history entry, sets the tenant's PaymentStatus to Paid,
        /// and — if BillingCycle is set — advances NextDueDateUtc by one cycle from whichever is
        /// later of the previous NextDueDateUtc or the payment date.</summary>
        Task<ApiResponse<TenantPaymentDto>> RecordPaymentAsync(Guid tenantId, RecordTenantPaymentRequest request, Guid? performedByAdminId);

        Task<ApiResponse<IReadOnlyList<TenantPaymentDto>>> GetPaymentsAsync(Guid tenantId);
    }
}
