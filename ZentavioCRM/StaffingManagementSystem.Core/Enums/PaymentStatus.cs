namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Manually-tracked billing status of a <see cref="Entities.Platform.Tenant"/>. There is no
    /// third-party payment gateway anywhere in this system — a platform admin sets this directly
    /// (or it's set for them when they record a payment via <see cref="Interfaces.ITenantBillingService"/>).
    /// Setting a tenant to <see cref="Overdue"/> automatically suspends it, the same reversible
    /// lock-out as <see cref="Interfaces.ITenantProvisioningService.SuspendAsync"/> — see
    /// <c>TenantBillingService.UpdateBillingAsync</c>. Marking it <see cref="Paid"/> again does
    /// NOT auto-reactivate the tenant; that stays a deliberate, separate admin action.
    /// </summary>
    public enum PaymentStatus
    {
        Unpaid = 1,
        Paid = 2,
        Overdue = 3,
    }
}
