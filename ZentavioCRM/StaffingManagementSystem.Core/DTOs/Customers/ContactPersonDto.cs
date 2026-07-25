namespace ZentavioCRM.Core.DTOs.Customers
{
    public class ContactPersonDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Designation { get; set; }

        public string? Department { get; set; }

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? WhatsApp { get; set; }

        public string? LinkedIn { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsDecisionMaker { get; set; }

        public string? Notes { get; set; }
    }

    public class SaveContactPersonRequest
    {
        public Guid? Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Designation { get; set; }

        public string? Department { get; set; }

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? WhatsApp { get; set; }

        public string? LinkedIn { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsDecisionMaker { get; set; }

        public string? Notes { get; set; }
    }
}
