namespace EduCollab.Application.Services.Notifications
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email (plain text, and HTML when provided). Failures are logged; implementors should not throw for notification mail.
        /// </summary>
        Task SendAsync(string to, EmailContent content, CancellationToken cancellationToken = default);
    }
}
