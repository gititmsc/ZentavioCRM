namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Lifecycle stage of a <see cref="Entities.Lead"/> as it moves toward conversion.
    /// </summary>
    public enum LeadStatus
    {
        New = 1,
        Assigned = 2,
        Contacted = 3,
        Qualified = 4,
        Nurturing = 5,
        ProposalSent = 6,
        Converted = 7,
        Lost = 8,
        Junk = 9
    }
}
