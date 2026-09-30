using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities.Platform
{
    /// <summary>
    /// A row in the Platform (master) database's admin registry — someone who can log into the
    /// Super Admin panel to provision/suspend/reactivate tenants and manage other platform admins.
    /// Deliberately separate from a tenant's own Users table: a platform admin has no tenant, and
    /// a tenant's Admin role has no platform access — the two are unrelated account systems that
    /// happen to share a password-hashing algorithm.
    /// </summary>
    public class PlatformAdmin
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>SuperAdmin (full access) or Support (read-only) — see <see cref="PlatformAdminRole"/>.
        /// Defaults to SuperAdmin so every admin created before roles existed keeps full access.</summary>
        public PlatformAdminRole Role { get; set; } = PlatformAdminRole.SuperAdmin;

        /// <summary>Consecutive failed login attempts since the last successful login or the last
        /// time a lockout expired. Reset to 0 on a successful login. See <see cref="LockedUntilUtc"/>.</summary>
        public int FailedLoginAttempts { get; set; }

        /// <summary>Set once <see cref="FailedLoginAttempts"/> crosses the lockout threshold; login
        /// is rejected until this passes, then the counter resets on the next attempt. Null when
        /// not currently locked out.</summary>
        public DateTime? LockedUntilUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
