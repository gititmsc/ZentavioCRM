namespace ZentavioCRM.Core.DTOs.Users
{
    /// <summary>Full user record shape used by the Users administration screens.</summary>
    public class UserDto
    {
        public Guid Id { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Mobile { get; set; }

        public Guid RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        public Guid? ReportingManagerId { get; set; }

        public string? ReportingManagerName { get; set; }

        public Guid? TerritoryId { get; set; }

        public string? TerritoryName { get; set; }

        /// <summary>Whether this user has an uploaded avatar — tells the frontend whether to render &lt;img src="/api/users/{id}/photo"&gt; or fall back to initials.</summary>
        public bool HasProfilePhoto { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }
    }
}
