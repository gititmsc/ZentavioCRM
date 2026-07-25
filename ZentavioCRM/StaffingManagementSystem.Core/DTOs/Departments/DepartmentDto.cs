namespace ZentavioCRM.Core.DTOs.Departments
{
    public class DepartmentDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? ParentDepartmentId { get; set; }

        public string? ParentDepartmentName { get; set; }

        public bool IsActive { get; set; }

        public int UserCount { get; set; }
    }
}
