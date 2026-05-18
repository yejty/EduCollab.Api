using EduCollab.Application.Services.Notifications;

namespace EduCollab.Api.Tests.Integration;

public sealed class SpyEmailSender : IEmailSender
{
    private readonly List<SentEmail> _emails = [];
    private readonly object _sync = new();

    public Task SendAsync(string to, EmailContent content, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _emails.Add(new SentEmail(to, content));
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_sync)
        {
            _emails.Clear();
        }
    }

    public SentEmail GetLatest(string to, string subject)
    {
        lock (_sync)
        {
            return _emails
                .LastOrDefault(email =>
                    string.Equals(email.To, to, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(email.Content.Subject, subject, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"No email found for '{to}' with subject '{subject}'.");
        }
    }
}

public sealed record SentEmail(string To, EmailContent Content);
