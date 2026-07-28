using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A member of the buying committee for an <see cref="Opportunity"/> — links an existing
    /// <see cref="ContactPerson"/> (belonging to the opportunity's Customer) to a deal-specific
    /// role (Champion, Economic Buyer, Blocker, etc.). One row per contact per opportunity.
    /// </summary>
    public class OpportunityContact
    {
        public Guid Id { get; set; }

        public Guid OpportunityId { get; set; }

        public Opportunity? Opportunity { get; set; }

        public Guid ContactPersonId { get; set; }

        public ContactPerson? ContactPerson { get; set; }

        public OpportunityContactRole Role { get; set; } = OpportunityContactRole.Other;

        public string? Notes { get; set; }
    }
}
