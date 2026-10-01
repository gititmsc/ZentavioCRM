namespace ZentavioCRM.Core.Entities
{
    /// <summary>Join row — one Tag attached to one Customer. Composite key (CustomerId, TagId); no surrogate Id needed, same convention as <see cref="RolePermission"/>.</summary>
    public class CustomerTag
    {
        public Guid CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public Guid TagId { get; set; }

        public Tag? Tag { get; set; }
    }
}
