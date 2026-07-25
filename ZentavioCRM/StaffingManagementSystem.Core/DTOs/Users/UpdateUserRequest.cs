using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Users
{
    public class UpdateUserRequest
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Mobile { get; set; }

        [Required(ErrorMessage = "A role must be assigned.")]
        public Guid RoleId { get; set; }

        public Guid? DepartmentId { get; set; }

        public Guid? ReportingManagerId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
