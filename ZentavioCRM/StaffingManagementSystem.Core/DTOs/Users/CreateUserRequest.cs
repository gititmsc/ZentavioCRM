using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Users
{
    public class CreateUserRequest
    {
        [Required, MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Mobile { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "A role must be assigned.")]
        public Guid RoleId { get; set; }

        public Guid? DepartmentId { get; set; }

        public Guid? ReportingManagerId { get; set; }

        public Guid? TerritoryId { get; set; }
    }
}
