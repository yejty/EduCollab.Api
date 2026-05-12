using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EduCollab.Application.Services.Notifications
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly IOptions<EmailSettings> _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task SendAsync(string to, EmailContent content, CancellationToken cancellationToken = default)
        {
            var settings = _options.Value;

            if (!settings.Enabled)
            {
                _logger.LogInformation("Email is disabled; skipped message to {To} with subject {Subject}.", to, content.Subject);
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.SmtpHost))
            {
                _logger.LogWarning("Email is enabled but SmtpHost is not set; skipped message to {To}.", to);
                return;
            }

            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Skipped email with subject {Subject}: recipient address is empty.", content.Subject);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromDisplayName, settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to.Trim()));
            message.Subject = content.Subject;

            if (!string.IsNullOrEmpty(content.HtmlBody))
            {
                message.Body = new MultipartAlternative
                {
                    new TextPart("plain") { Text = content.PlainText },
                    new TextPart("html") { Text = content.HtmlBody },
                };
            }
            else
                message.Body = new TextPart("plain") { Text = content.PlainText };

            try
            {
                using var client = new SmtpClient();
                var secure = settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secure, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(settings.UserName))
                    await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);

                await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
                await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}.", to, content.Subject);
            }
        }
    }
}
