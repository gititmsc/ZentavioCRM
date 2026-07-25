using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Platform
{
    public class TenantDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Subdomain { get; set; } = string.Empty;

        public string DatabaseName { get; set; } = string.Empty;

        public TenantStatus Status { get; set; }

        public string AdminEmail { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
