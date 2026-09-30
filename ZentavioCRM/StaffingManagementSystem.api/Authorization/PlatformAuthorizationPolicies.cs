namespace ZentavioCRM.Api.Authorization
{
    /// <summary>
    /// Names shared between Program.cs (where the "PlatformAdmin" JWT bearer scheme and
    /// authorization policy are registered) and every controller under Controllers/Platform that
    /// requires it — kept as constants so the two sides can't drift out of sync via a typo.
    /// </summary>
    public static class PlatformAuthorizationPolicies
    {
        /// <summary>Name of both the JWT bearer authentication scheme and the authorization policy for Platform Admin sessions — distinct from the default (tenant user) scheme.</summary>
        public const string PlatformAdmin = "PlatformAdmin";

        /// <summary>Authorization policy for mutating platform actions (provision/suspend/reactivate/stop
        /// a tenant, billing, quick admin actions, managing other platform admins) — requires the
        /// "PlatformAdmin" scheme AND a role claim of SuperAdmin. A Support-role admin authenticates
        /// fine under the PlatformAdmin policy (so read endpoints work) but is rejected here.</summary>
        public const string PlatformSuperAdmin = "PlatformSuperAdmin";
    }
}
