using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Departments
{
    /// <summary>Shared shape for creating and updating a Department.</summary>
    public class SaveDepartmentRequest
    {
        [Required(ErrorMessage = "Department name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public Guid? ParentDepartmentId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
