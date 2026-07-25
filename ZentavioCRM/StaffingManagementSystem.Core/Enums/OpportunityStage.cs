namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Pipeline stage of an <see cref="Entities.Opportunity"/>, per the Sales Process Flow
    /// defined in the CRM SRS (Phase 6 — Opportunity Management).
    /// </summary>
    public enum OpportunityStage
    {
        Qualification = 1,
        Discovery = 2,
        Proposal = 3,
        Negotiation = 4,
        VerbalCommit = 5,
        ClosedWon = 6,
        ClosedLost = 7
    }
}
