namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Lifecycle status of a <see cref="Entities.SalesOrder"/>, per CRM SRS Phase 6, section 6
    /// "Sales Order Management". PartiallyDelivered/Delivered are derived automatically from the
    /// line items' DeliveredQuantity rather than set directly by the user.
    /// </summary>
    public enum SalesOrderStatus
    {
        Draft = 1,
        Confirmed = 2,
        PartiallyDelivered = 3,
        Delivered = 4,
        Cancelled = 5
    }
}
