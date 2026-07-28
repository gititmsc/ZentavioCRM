namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Lifecycle status of a <see cref="Entities.Quotation"/>, per CRM SRS Phase 6, section 5
    /// "Quotation Management".
    /// </summary>
    public enum QuotationStatus
    {
        Draft = 1,
        Sent = 2,
        Accepted = 3,
        Rejected = 4,
        Expired = 5
    }
}
