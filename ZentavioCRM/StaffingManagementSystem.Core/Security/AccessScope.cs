using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Security
{
    /// <summary>
    /// Resolved record-visibility context for a single user on a single request — how much of the
    /// Leads/Customers/Opportunities record-set they're allowed to see, per their Role's
    /// <see cref="VisibilityScope"/>. Built by IAccessScopeService (StaffingManagementSystem.Services)
    /// and consumed in two ways: repositories build SQL-translatable Where() clauses directly from
    /// Scope/UserId/TeamUserIds/DelegatedFromUserIds (do NOT call CanSee inside a LINQ-to-Entities
    /// query — method calls on a plain C# object don't translate to SQL), while services do simple
    /// in-memory <see cref="CanSee"/> checks on a single already-fetched record.
    /// </summary>
    public class AccessScope
    {
        public VisibilityScope Scope { get; init; }

        public Guid UserId { get; init; }

        /// <summary>Every user ID sharing the current user's Department (including themselves). Only populated when Scope == Team.</summary>
        public IReadOnlySet<Guid> TeamUserIds { get; init; } = new HashSet<Guid>();

        /// <summary>User IDs the current user is actively covering for via an in-window delegation. A record assigned to one of these users is visible regardless of Scope — delegation is an override layered on top of, not gated by, Own/Team/All.</summary>
        public IReadOnlySet<Guid> DelegatedFromUserIds { get; init; } = new HashSet<Guid>();

        /// <summary>In-memory visibility check for a single already-fetched record. Not for use inside an EF Core query — see class remarks.</summary>
        public bool CanSee(Guid? assignedToUserId, Guid? createdByUserId)
        {
            if (assignedToUserId is not null && DelegatedFromUserIds.Contains(assignedToUserId.Value))
            {
                return true;
            }

            if (assignedToUserId is null && createdByUserId is not null && DelegatedFromUserIds.Contains(createdByUserId.Value))
            {
                return true;
            }

            return Scope switch
            {
                VisibilityScope.All => true,
                VisibilityScope.Own => assignedToUserId == UserId || (assignedToUserId is null && createdByUserId == UserId),
                VisibilityScope.Team => (assignedToUserId is not null && TeamUserIds.Contains(assignedToUserId.Value))
                    || (assignedToUserId is null && createdByUserId is not null && TeamUserIds.Contains(createdByUserId.Value)),
                _ => true,
            };
        }
    }
}
