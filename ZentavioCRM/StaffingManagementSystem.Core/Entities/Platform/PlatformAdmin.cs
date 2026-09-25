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

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
