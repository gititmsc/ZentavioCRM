namespace ZentavioCRM.Core.Entities
{
    /// <summary>Join row — one Tag attached to one Lead. Composite key (LeadId, TagId); no surrogate Id needed, same convention as <see cref="RolePermission"/>.</summary>
    public class LeadTag
    {
        public Guid LeadId { get; set; }

        public Lead? Lead { get; set; }

        public Guid TagId { get; set; }

        public Tag? Tag { get; set; }
    }
}
