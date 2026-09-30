namespace ZentavioCRM.Core.DTOs.Platform
{
    /// <summary>One match from <see cref="Interfaces.IGlobalSearchService"/> — a tenant or a
    /// platform admin whose name/email/subdomain contains the search term. Deliberately thin: just
    /// enough for the PlatformWeb topbar search dropdown to display a result and link to it.</summary>
    public class GlobalSearchResultDto
    {
        /// <summary>"Tenant" or "PlatformAdmin".</summary>
        public string Type { get; set; } = string.Empty;

        public Guid Id { get; set; }

        /// <summary>Primary display text — tenant name, or platform admin full name.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Secondary display text — tenant subdomain/admin email, or platform admin email.</summary>
        public string SubLabel { get; set; } = string.Empty;
    }
}
