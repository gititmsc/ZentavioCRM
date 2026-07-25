namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Purpose of a <see cref="Entities.CustomerAddress"/> record.
    /// </summary>
    public enum AddressType
    {
        Billing = 1,
        Shipping = 2,
        RegisteredOffice = 3,
        BranchOffice = 4,
        Warehouse = 5,
        Site = 6
    }
}
