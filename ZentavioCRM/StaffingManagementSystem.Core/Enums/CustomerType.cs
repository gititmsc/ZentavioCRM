namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Classifies the commercial relationship a <see cref="Entities.Customer"/> record represents.
    /// </summary>
    public enum CustomerType
    {
        Prospect = 1,
        Individual = 2,
        Business = 3,
        Vendor = 4,
        Partner = 5,
        Supplier = 6,
        Distributor = 7,
        Dealer = 8,
        Franchise = 9,
        Consultant = 10
    }
}
