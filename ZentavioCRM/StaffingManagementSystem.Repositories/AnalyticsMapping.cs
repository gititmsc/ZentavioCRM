namespace ZentavioCRM.Repositories
{
    /// <summary>Tiny shared helpers for projecting entities into the analytics engine's flat record shape.</summary>
    internal static class AnalyticsMapping
    {
        /// <summary>"First Last" for an owner, or null when unassigned/blank — the engine renders null as "(none)".</summary>
        public static string? Owner(string? first, string? last)
        {
            var name = $"{first} {last}".Trim();
            return name.Length == 0 ? null : name;
        }
    }
}
