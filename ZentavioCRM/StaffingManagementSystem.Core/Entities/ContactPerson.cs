namespace ZentavioCRM.Core.Entities
{
    /// <summary>An individual contact belonging to a <see cref="Customer"/>. A customer may have unlimited contacts.</summary>
    public class ContactPerson
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public Customer? Customer { get; set; }

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

        public DateTime CreatedAtUtc { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
