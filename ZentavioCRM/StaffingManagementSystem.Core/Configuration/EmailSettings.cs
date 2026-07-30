namespace ZentavioCRM.Core.Configuration
{
    /// <summary>
    /// Strongly typed binding of the "Email" configuration section — SMTP relay used for
    /// transactional emails (currently just password-reset links).
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "Email";

        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        public string SmtpUsername { get; set; } = string.Empty;

        public string SmtpPassword { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;

        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// When true, every outgoing email is redirected to <see cref="TestToEmailAddress"/>
        /// regardless of the intended recipient — a safety net for QA/staging environments so test
        /// runs can never accidentally email a real user.
        /// </summary>
        public bool EnableTestMode { get; set; }

        public string TestToEmailAddress { get; set; } = string.Empty;
    }
}
