namespace ZentavioCRM.Core.Enums
{
    /// <summary>How often a tenant is billed. Purely informational/manual — drives the automatic
    /// NextDueDateUtc rollover in <see cref="Interfaces.ITenantBillingService.RecordPaymentAsync"/>
    /// when a payment is recorded, nothing else.</summary>
    public enum BillingCycle
    {
        OneTime = 1,
        Monthly = 2,
        Yearly = 3,
    }
}
