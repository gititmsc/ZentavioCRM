namespace ZentavioCRM.Core.DTOs.Territories
{
    public class TerritoryDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? ParentTerritoryId { get; set; }

        public string? ParentTerritoryName { get; set; }

        public bool IsActive { get; set; }

        public int UserCount { get; set; }

        public int LeadCount { get; set; }
    }
}
