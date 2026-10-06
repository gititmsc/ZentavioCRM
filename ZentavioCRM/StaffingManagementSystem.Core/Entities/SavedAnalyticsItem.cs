namespace ZentavioCRM.Core.Entities
{
    public enum SavedAnalyticsKind
    {
        Dashboard = 1,
        Report = 2,
    }

    /// <summary>
    /// A user-built custom Dashboard or saved Report. <see cref="ConfigJson"/> is deliberately opaque to the
    /// server (the widget layout / report definition is owned by the UI); every query it describes is
    /// re-validated and access-scoped by the analytics engine when it actually runs.
    /// </summary>
    public class SavedAnalyticsItem
    {
        public Guid Id { get; set; }

        public SavedAnalyticsKind Kind { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid OwnerUserId { get; set; }

        public User? OwnerUser { get; set; }

        /// <summary>Visible (read/run only) to every user in the tenant. Setting it requires Analytics.ManageShared.</summary>
        public bool IsShared { get; set; }

        public string ConfigJson { get; set; } = "{}";

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
