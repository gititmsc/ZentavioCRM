namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Manually-set relationship health/engagement indicator for a <see cref="Entities.Customer"/>.
    /// Distinct from <see cref="Entities.Customer.Rating"/> (a freeform deal-quality label) — this is a
    /// dedicated, structured field purpose-built for account-health tracking (e.g. flagging at-risk accounts).
    /// </summary>
    public enum CustomerHealthStatus
    {
        Hot = 1,
        Warm = 2,
        Cold = 3,
        AtRisk = 4
    }
}
