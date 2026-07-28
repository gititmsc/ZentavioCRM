namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A structured sales/service territory, supporting a self-referencing hierarchy
    /// (Territory -&gt; Sub-territory), same pattern as <see cref="Department"/>. This supersedes
    /// the free-text <see cref="Lead.Territory"/> field going forward — that field stays as
    /// legacy free text (no data migration, no removal, to avoid breaking existing CSV
    /// import/export), while <see cref="Lead.TerritoryId"/> and <see cref="User.TerritoryId"/>
    /// reference this structured entity instead.
    /// </summary>
    public class Territory
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? ParentTerritoryId { get; set; }

        public Territory? ParentTerritory { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<Territory> ChildTerritories { get; set; } = new List<Territory>();
    }
}
