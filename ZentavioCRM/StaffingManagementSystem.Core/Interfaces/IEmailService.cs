namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Sends transactional emails (currently just password-reset links). Implemented in the
    /// Infrastructure layer over SMTP.
    /// </summary>
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
