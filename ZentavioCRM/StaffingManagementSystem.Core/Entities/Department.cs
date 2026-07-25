namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// An organizational unit within a <see cref="Company"/>. Supports a self-referencing
    /// hierarchy (Department -> Sub-department) per the org-structure requirements in the SRS.
    /// </summary>
    public class Department
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public Company? Company { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? ParentDepartmentId { get; set; }

        public Department? ParentDepartment { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
