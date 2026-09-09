namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Whether a Product & Service Catalog entry is a physical/orderable product or a
    /// billable service, per the CRM SRS (Phase 5 — Product & Service Catalog). Drives which
    /// of the type-specific fields on <see cref="Entities.Product"/> are meaningful.
    /// </summary>
    public enum ProductType
    {
        Product = 1,
        Service = 2
    }
}
