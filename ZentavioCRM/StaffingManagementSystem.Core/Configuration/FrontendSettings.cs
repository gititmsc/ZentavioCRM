namespace ZentavioCRM.Core.Configuration
{
    /// <summary>
    /// Strongly typed binding of the root-level "FrontendBaseUrl" config value — the base URL of
    /// the SPA, used to build links back to it (e.g. the password-reset link in an email) since
    /// the Api and the SPA are served from separate origins.
    /// </summary>
    public class FrontendSettings
    {
        public string FrontendBaseUrl { get; set; } = string.Empty;
    }
}
