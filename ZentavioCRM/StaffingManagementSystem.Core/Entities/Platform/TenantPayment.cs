namespace ZentavioCRM.Core.Entities.Platform
{
    /// <summary>
    /// A single manually-recorded payment entry against a tenant's billing history — the ledger
    /// backing "Full manual payment history". Append-only: there is no edit/delete for a recorded
    /// payment, same append-only philosophy as <see cref="PlatformAuditLog"/>. Recording one of
    /// these is what sets <see cref="Tenant.PaymentStatus"/> to Paid and advances
    /// <see cref="Tenant.NextDueDateUtc"/> — see <see cref="Interfaces.ITenantBillingService.RecordPaymentAsync"/>.
    /// There is no payment gateway involved anywhere here — this is purely a record that a
    /// platform admin says a payment happened.
    /// </summary>
    public class TenantPayment
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public decimal Amount { get; set; }

        /// <summary>ISO 4217 code, e.g. "USD".</summary>
        public string Currency { get; set; } = "USD";

        public DateTime PaidAtUtc { get; set; }

        public string? Note { get; set; }

        /// <summary>Null if the admin account was later deleted.</summary>
        public Guid? RecordedByAdminId { get; set; }

        public PlatformAdmin? RecordedByAdmin { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
