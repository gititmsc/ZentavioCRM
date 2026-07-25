namespace ZentavioCRM.Core.DTOs.Auth
{
    /// <summary>
    /// Safe, public projection of a <see cref="Entities.User"/> returned to clients.
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        /// <summary>Flat list of permission codes granted to the user's role, e.g. "Leads.Create".
        /// Lets the frontend show/hide navigation and actions without a second round trip.</summary>
        public List<string> Permissions { get; set; } = [];
    }
}
