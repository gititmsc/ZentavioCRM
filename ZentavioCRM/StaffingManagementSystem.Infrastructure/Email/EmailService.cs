using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Infrastructure.Email
{
    /// <inheritdoc cref="IEmailService"/>
    /// <remarks>
    /// Uses the built-in <see cref="SmtpClient"/> rather than a third-party mail library, deliberately —
    /// it needs no additional NuGet package, which keeps this dependency-free and easy to verify.
    /// </remarks>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            // EnableTestMode redirects every outgoing email to a single known inbox regardless of
            // the real recipient — a safety net so QA/staging environments can never accidentally
            // email a real user while exercising the password-reset flow.
            var actualRecipient = _settings.EnableTestMode ? _settings.TestToEmailAddress : toEmail;

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = _settings.EnableTestMode ? $"[TEST → {toEmail}] {subject}" : subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(actualRecipient);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
            };

            await client.SendMailAsync(message);
        }
    }
}
