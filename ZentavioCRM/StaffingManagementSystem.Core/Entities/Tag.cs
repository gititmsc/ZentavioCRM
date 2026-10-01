namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A reusable, tenant-wide label that can be attached to any number of Leads and/or Customers
    /// (see <see cref="LeadTag"/>/<see cref="CustomerTag"/>) — powers the Tag Manager screen and the
    /// tag picker on the Lead/Customer forms. Distinct from <see cref="Customer.Tags"/>, the original
    /// freeform comma-separated text field: that stays exactly as-is (CSV import/export still reads
    /// and writes it) so existing data and workflows are untouched, while this structured system is
    /// the new, recommended way to tag records going forward.
    /// </summary>
    public class Tag
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Optional hex color (e.g. "#FF6B6B") for the tag's chip in the UI. Null renders with a default neutral color.</summary>
        public string? Color { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
