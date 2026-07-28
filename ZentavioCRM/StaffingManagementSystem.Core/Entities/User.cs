namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A user account that can authenticate into ZentavioCRM.
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        /// <summary>PBKDF2 password hash, stored as "{iterations}.{saltBase64}.{hashBase64}".</summary>
        public string PasswordHash { get; set; } = string.Empty;

        public Guid RoleId { get; set; }

        public Role? Role { get; set; }

        public Guid? DepartmentId { get; set; }

        public Department? Department { get; set; }

        public Guid? ReportingManagerId { get; set; }

        public User? ReportingManager { get; set; }

        public Guid? TerritoryId { get; set; }

        public Territory? Territory { get; set; }

        /// <summary>Raw image bytes for the user's avatar. Stored directly on User (not via the generic Document entity) to avoid an extra lookup/auth round-trip per avatar when rendering many users in a list at once.</summary>
        public byte[]? ProfilePhotoContent { get; set; }

        public string? ProfilePhotoContentType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
