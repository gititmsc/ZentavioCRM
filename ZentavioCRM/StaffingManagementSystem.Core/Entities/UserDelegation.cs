namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// An out-of-office delegation: while today's date falls within [StartDateUtc, EndDateUtc],
    /// DelegateUser temporarily (a) sees DelegatorUser's assigned Leads/Customers/Opportunities
    /// regardless of the delegate's own Role.VisibilityScope (see <see cref="Security.AccessScope"/>
    /// .DelegatedFromUserIds), and (b) receives DelegatorUser's due-date/follow-up reminder
    /// notifications instead of (in addition to) the delegator themselves.
    /// </summary>
    public class UserDelegation
    {
        public Guid Id { get; set; }

        /// <summary>The user who is out of office / delegating their records.</summary>
        public Guid DelegatorUserId { get; set; }

        public User? DelegatorUser { get; set; }

        /// <summary>The user temporarily covering for the delegator.</summary>
        public Guid DelegateUserId { get; set; }

        public User? DelegateUser { get; set; }

        public DateTime StartDateUtc { get; set; }

        public DateTime EndDateUtc { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
