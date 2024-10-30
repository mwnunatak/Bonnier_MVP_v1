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
    }
}