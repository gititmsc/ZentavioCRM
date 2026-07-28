namespace ZentavioCRM.Core.DTOs.Delegations
{
    public class UserDelegationDto
    {
        public Guid Id { get; set; }

        public Guid DelegatorUserId { get; set; }

        public string DelegatorUserName { get; set; } = string.Empty;

        public Guid DelegateUserId { get; set; }

        public string DelegateUserName { get; set; } = string.Empty;

        public DateTime StartDateUtc { get; set; }

        public DateTime EndDateUtc { get; set; }

        public string? Notes { get; set; }

        /// <summary>Whether today's date currently falls within [StartDateUtc, EndDateUtc] — i.e. the delegation is in effect right now.</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
