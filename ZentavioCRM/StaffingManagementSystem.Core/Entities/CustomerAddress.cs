using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>A physical address belonging to a <see cref="Customer"/>. A customer may have multiple addresses.</summary>
    public class CustomerAddress
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public AddressType Type { get; set; } = AddressType.Billing;

        public string Line1 { get; set; } = string.Empty;

        public string? Line2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? PostalCode { get; set; }

        public bool IsPrimary { get; set; }
    }
}
