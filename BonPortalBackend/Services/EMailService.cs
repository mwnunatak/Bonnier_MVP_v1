using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure;

namespace BonPortalBackend.Services
{
    public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, int verificationCode);
    Task SendEmailChangeNotificationAsync(string oldEmail, string newEmail, string safetyLink);
}

    public class EmailService : IEmailService
    {
        private readonly string _connectionString;
        private readonly string _senderEmail;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _connectionString = configuration["AZURE_COMMUNICATION_CONNECTION_STRING"]
                ?? throw new InvalidOperationException("AZURE_COMMUNICATION_CONNECTION_STRING not found in environment variables.");
            _senderEmail = configuration["AZURE_COMMUNICATION_SENDER_EMAIL"]
                ?? throw new InvalidOperationException("AZURE_COMMUNICATION_SENDER_EMAIL not found in environment variables.");
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, int verificationCode)
        {
            try
            {
                _logger.LogInformation($"Attempting to send verification email to {toEmail}");
                var emailClient = new EmailClient(_connectionString);
                var emailMessage = new EmailMessage(
                    senderAddress: _senderEmail,
                    recipientAddress: toEmail,
                    content: new EmailContent(
                        subject: "E-Mail Verifizierung")
                    {
                        PlainText = $"Ihr Verifizierungscode lautet: {verificationCode}",
                        Html = $"<h2>E-Mail Verifizierung</h2><p>Ihr Verifizierungscode lautet: <strong>{verificationCode}</strong></p>"
                    }
                );

                await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                _logger.LogInformation($"Verification email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending verification email: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task SendEmailChangeNotificationAsync(string oldEmail, string newEmail, string safetyLink)
{
    try
    {
        _logger.LogInformation($"Sending email change notification to {oldEmail}");
        var emailClient = new EmailClient(_connectionString);
        var emailMessage = new EmailMessage(
            senderAddress: _senderEmail,
            recipientAddress: oldEmail,
            content: new EmailContent(
                subject: "Änderung Ihrer E-Mail-Adresse")
            {
                PlainText = $"Jemand hat beantragt, dass Ihre Belege digital von Bonnier bearbeitet werden. Ihre E-Mail-Adresse für den digitalen Abrechnungsversand wurde von {oldEmail} zu {newEmail} geändert. Falls Sie diese Änderung nicht vorgenommen haben, klicken Sie bitte hier: {safetyLink}",
                Html = $@"
                    <h2>Änderung Ihrer E-Mail-Adresse</h2>
                    <p>Jemand hat beantragt, dass Ihre Belege digital von Bonnier bearbeitet werden.</p>
                    <p>Ihre E-Mail-Adresse wurde von <strong>{oldEmail}</strong> zu <strong>{newEmail}</strong> geändert.</p>
                    <p>Falls Sie diese Änderung nicht vorgenommen haben, klicken Sie bitte <a href='{safetyLink}'>hier</a>.</p>"
            }
        );

        await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
        _logger.LogInformation($"Email change notification sent successfully to {oldEmail}");
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error sending email change notification: {ex.Message}");
        throw;
    }
}
    }
}