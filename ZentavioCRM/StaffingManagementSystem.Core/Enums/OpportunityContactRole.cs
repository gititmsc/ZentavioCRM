namespace ZentavioCRM.Core.Enums
{
    /// <summary>The role a buying-committee member plays on a specific Opportunity (deal-specific, not a property of the ContactPerson itself — the same contact could be a Champion on one deal and a Blocker on another).</summary>
    public enum OpportunityContactRole
    {
        Champion = 1,
        EconomicBuyer = 2,
        Blocker = 3,
        Influencer = 4,
        DecisionMaker = 5,
        TechnicalEvaluator = 6,
        Other = 7
    }
}
