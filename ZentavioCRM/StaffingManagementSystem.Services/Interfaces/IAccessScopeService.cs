using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>Resolves the record-visibility (Own/Team/All) context for a user, based on their Role's VisibilityScope.</summary>
    public interface IAccessScopeService
    {
        /// <summary>Loads the user, resolves their Role's VisibilityScope, and (only when Team) their department's member IDs.
        /// Falls back to Own-only scope if the user can't be found (fail closed, not open).</summary>
        Task<AccessScope> GetForUserAsync(Guid userId);
    }
}
