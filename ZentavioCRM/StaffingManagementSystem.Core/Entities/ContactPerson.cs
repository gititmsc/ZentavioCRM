using ZentavioCRM.Core.Enums;

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

        public PreferredContactMethod? PreferredContactMethod { get; set; }

        /// <summary>Used for birthday relationship-touch reminders. Only month/day are meaningful for the reminder check — the year is whatever was entered (or unknown).</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>e.g. a business relationship anniversary or a personal one the contact has shared — used for anniversary relationship-touch reminders. Only month/day are meaningful for the reminder check.</summary>
        public DateTime? AnniversaryDate { get; set; }

        /// <summary>Calendar year a birthday reminder was last sent for this contact, so it isn't re-sent every day within the same year. Compared against DateOfBirth's month/day, not year.</summary>
        public int? BirthdayReminderSentYear { get; set; }

        /// <summary>Calendar year an anniversary reminder was last sent for this contact.</summary>
        public int? AnniversaryReminderSentYear { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
