using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Customers
{
    public class CustomerAddressDto
    {
        public Guid Id { get; set; }

        public AddressType Type { get; set; }

        public string Line1 { get; set; } = string.Empty;

        public string? Line2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? PostalCode { get; set; }

        public bool IsPrimary { get; set; }
    }

    public class SaveCustomerAddressRequest
    {
        public Guid? Id { get; set; }

        public AddressType Type { get; set; }

        public string Line1 { get; set; } = string.Empty;

        public string? Line2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? PostalCode { get; set; }

        public bool IsPrimary { get; set; }
    }
}
