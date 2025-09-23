using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Wayward.Domain;
using Wayward.Service.Interface;

namespace Wayward.Service.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;

        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        // kept sync + bool return, as in your original
        public bool SendEmailAsync(EmailMessage allMails)
        {
            if (allMails == null) throw new ArgumentNullException(nameof(allMails));
            if (string.IsNullOrWhiteSpace(allMails.MailTo)) throw new ArgumentException("MailTo is required.", nameof(allMails));

            // From/Sender come from EmailSettings (your Gmail)
            var fromDisplay = string.IsNullOrWhiteSpace(_mailSettings.FromDisplayName)
                ? "Wayward & Co"
                : _mailSettings.FromDisplayName;

            var fromAddress = string.IsNullOrWhiteSpace(_mailSettings.FromAddress)
                ? _mailSettings.SmtpUserName
                : _mailSettings.FromAddress;

            var emailMessage = new MimeMessage
            {
                Sender = new MailboxAddress(fromDisplay, fromAddress),
                Subject = allMails.Subject ?? "(no subject)"
            };

            emailMessage.From.Add(new MailboxAddress(fromDisplay, fromAddress));
            emailMessage.To.Add(new MailboxAddress(allMails.MailTo, allMails.MailTo));

            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = string.IsNullOrWhiteSpace(allMails.Content) ? "(empty message)" : allMails.Content
            };

            try
            {
                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                // Use your configured port and TLS style
                // Gmail: 587 + STARTTLS (UseSsl=false) or 465 + SSL (UseSsl=true)
                var port = _mailSettings.SmtpPort > 0 ? _mailSettings.SmtpPort : 587;
                var secure = _mailSettings.UseSsl
                    ? SecureSocketOptions.SslOnConnect        // e.g., port 465
                    : SecureSocketOptions.StartTlsWhenAvailable; // e.g., port 587

                smtp.Connect(_mailSettings.SmtpServer, port, secure);

                if (!string.IsNullOrWhiteSpace(_mailSettings.SmtpUserName))
                {
                    smtp.Authenticate(_mailSettings.SmtpUserName, _mailSettings.SmtpPassword);
                }

                smtp.Send(emailMessage);
                smtp.Disconnect(true);
                return true;
            }
            catch
            {
                // Preserve original stack
                throw;
            }
        }
    }
}
