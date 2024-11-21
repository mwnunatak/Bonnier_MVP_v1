using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure;
using Microsoft.AspNetCore.Http;

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
        private readonly string _baseTemplate;
        private readonly string _logoUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _connectionString = configuration["AZURE_COMMUNICATION_CONNECTION_STRING"]
                ?? throw new InvalidOperationException("AZURE_COMMUNICATION_CONNECTION_STRING not found.");
            _senderEmail = configuration["AZURE_COMMUNICATION_SENDER_EMAIL"]
                ?? throw new InvalidOperationException("AZURE_COMMUNICATION_SENDER_EMAIL not found.");
            _logger = logger;

            // Construct the logo URL based on the application's base URL
            var request = httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            _logoUrl = $"{baseUrl}/images/bonnier_logo.png";
            
            _baseTemplate = LoadEmailTemplate();
        }

        public async Task SendVerificationEmailAsync(string toEmail, int verificationCode)
        {
            var germanContent = $@"
                <h2>E-Mail Verifizierung</h2>
                <p>Willkommen bei Bonnier!</p>
                <p>Um Ihre E-Mail-Adresse zu verifizieren, geben Sie bitte folgenden Code ein:</p>
                <h3 style='color: #4C9F70; font-size: 24px;'>{verificationCode}</h3>
                <p>Dieser Code ist aus Sicherheitsgründen nur für kurze Zeit gültig.</p>";

            var englishContent = $@"
                <h2>Email Verification</h2>
                <p>Welcome to Bonnier!</p>
                <p>To verify your email address, please enter the following code:</p>
                <h3 style='color: #4C9F70; font-size: 24px;'>{verificationCode}</h3>
                <p>For security reasons, this code is only valid for a short time.</p>";

            var htmlContent = _baseTemplate
                .Replace("[GERMAN_CONTENT]", germanContent)
                .Replace("[ENGLISH_CONTENT]", englishContent)
                .Replace("[LOGO_URL]", _logoUrl);

            try
            {
                _logger.LogInformation($"Attempting to send verification email to {toEmail}");
                var emailClient = new EmailClient(_connectionString);
                var emailMessage = new EmailMessage(
                    senderAddress: _senderEmail,
                    recipientAddress: toEmail,
                    content: new EmailContent(
                        subject: "E-Mail Verifizierung / Email Verification")
                    {
                        Html = htmlContent,
                        PlainText = $"Ihr Verifizierungscode lautet: {verificationCode}\n\nYour verification code is: {verificationCode}"
                    }
                );

                await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                _logger.LogInformation($"Verification email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending verification email: {ex.Message}");
                throw;
            }
        }

        public async Task SendEmailChangeNotificationAsync(string oldEmail, string newEmail, string safetyLink)
        {
            var germanContent = $@"
                <h2>Änderung Ihrer E-Mail-Adresse</h2>
                <p>Jemand hat beantragt, dass Ihre Belege digital von Bonnier bearbeitet werden.</p>
                <p>Ihre E-Mail-Adresse wurde von <strong>{oldEmail}</strong> zu <strong>{newEmail}</strong> geändert.</p>
                <p>Falls Sie diese Änderung nicht vorgenommen haben, klicken Sie bitte hier:</p>
                <a href='{safetyLink}' class='button'>Änderung zurücksetzen</a>";

            var englishContent = $@"
                <h2>Email Address Change</h2>
                <p>Someone has requested that your documents be processed digitally by Bonnier.</p>
                <p>Your email address has been changed from <strong>{oldEmail}</strong> to <strong>{newEmail}</strong>.</p>
                <p>If you did not make this change, please click here:</p>
                <a href='{safetyLink}' class='button'>Reset Change</a>";

            var htmlContent = _baseTemplate
                .Replace("[GERMAN_CONTENT]", germanContent)
                .Replace("[ENGLISH_CONTENT]", englishContent)
                .Replace("[LOGO_URL]", _logoUrl);

            try
            {
                _logger.LogInformation($"Sending email change notification to {oldEmail}");
                var emailClient = new EmailClient(_connectionString);
                var emailMessage = new EmailMessage(
                    senderAddress: _senderEmail,
                    recipientAddress: oldEmail,
                    content: new EmailContent(
                        subject: "Änderung Ihrer E-Mail-Adresse / Email Address Change")
                    {
                        Html = htmlContent,
                        PlainText = $"Ihre E-Mail-Adresse wurde von {oldEmail} zu {newEmail} geändert. Falls Sie diese Änderung nicht vorgenommen haben, besuchen Sie bitte: {safetyLink}\n\nYour email address has been changed from {oldEmail} to {newEmail}. If you did not make this change, please visit: {safetyLink}"
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

        private string LoadEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
            color: #333333;
        }
        .email-container {
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }
        .logo {
            margin-bottom: 30px;
        }
        .logo img {
            height: 40px;
        }
        .content {
            margin-bottom: 30px;
        }
        .language-divider {
            border-top: 1px solid #E5E5E5;
            margin: 25px 0;
        }
        .footer {
            font-size: 12px;
            color: #666666;
            border-top: 1px solid #E5E5E5;
            margin-top: 30px;
            padding-top: 20px;
        }
        .button {
            display: inline-block;
            background-color: #4C9F70;
            color: white !important;
            padding: 12px 25px;
            text-decoration: none;
            border-radius: 5px;
            margin: 15px 0;
        }
        .language-notice {
            font-style: italic;
            color: #666666;
            margin-bottom: 20px;
        }
        a {
            color: #4C9F70;
            text-decoration: none;
        }
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""logo"">
            <img src=""[LOGO_URL]"" alt=""Bonnier"" />
        </div>
        
        <div class=""language-notice"">
            English below
        </div>
        
        <div class=""content"">
            [GERMAN_CONTENT]
        </div>

        <div class=""language-divider""></div>

        <div class=""content"">
            [ENGLISH_CONTENT]
        </div>

        <div class=""footer"">
            <p>Bonnier Media Deutschland GmbH<br>
            Friedrichstraße 9<br>
            80801 München<br>
            Tel.: +49 (0)89 - 3810060</p>
            
            <p><a href=""https://www.bonnier.de/impressum/"">Impressum</a></p>
        </div>
    </div>
</body>
</html>";
        }
    }
}