using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace IMS.Api.Services
{
    public class BrevoSmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public BrevoSmtpEmailSender(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string? plainTextBody = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
                throw new InvalidOperationException("SMTP host is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.SmtpUsername))
                throw new InvalidOperationException("SMTP username is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.SmtpPassword))
                throw new InvalidOperationException("SMTP password is not configured.");

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
                throw new InvalidOperationException("From email is not configured.");

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _settings.FromName,
                _settings.FromEmail
            ));

            if (!string.IsNullOrWhiteSpace(_settings.ReplyToEmail))
            {
                message.ReplyTo.Add(new MailboxAddress(
                    _settings.ReplyToName ?? _settings.FromName,
                    _settings.ReplyToEmail
                ));
            }

            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = plainTextBody ?? htmlBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                _settings.SmtpUsername,
                _settings.SmtpPassword
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}