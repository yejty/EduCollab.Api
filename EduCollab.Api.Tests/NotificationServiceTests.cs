using EduCollab.Application.Repositories.Notifications;
using EduCollab.Application.Services.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace EduCollab.Api.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public void EmailConfirmation_RendersActionButton()
    {
        var content = EduCollabEmailTemplates.EmailConfirmation(
            "http://localhost:3000/confirm?token=abc",
            "abc",
            validForHours: 24);

        Assert.Contains("Confirm email", content.HtmlBody);
        Assert.Contains("http://localhost:3000/confirm?token=abc", content.HtmlBody);
    }

    [Fact]
    public async Task SendAsync_PersistsAndMarksSent()
    {
        var repository = new FakeNotificationRepository();
        var emailSender = new RecordingEmailSender();
        var service = new NotificationService(repository, emailSender, NullLogger<NotificationService>.Instance);

        var message = NotificationMessage.Create(
            "user@example.com",
            NotificationType.PasswordChanged,
            EduCollabEmailTemplates.PasswordChanged(),
            actions: new[] { new NotificationAction("Review account", "http://localhost:3000/account") });

        await service.SendAsync(message);

        Assert.Single(repository.Inserted);
        Assert.Single(repository.Sent);
        Assert.Empty(repository.Failed);
        Assert.Contains("Review account", repository.Inserted[0].MetadataJson);
        Assert.Single(emailSender.Sent);
        Assert.Equal("user@example.com", emailSender.Sent[0].To);
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        private long _nextId;

        public List<(long Id, NotificationMessage Message, string? MetadataJson)> Inserted { get; } = [];
        public List<long> Sent { get; } = [];
        public List<(long Id, string Error)> Failed { get; } = [];

        public Task<long> InsertPendingAsync(
            NotificationMessage message,
            string? metadataJson,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            var id = ++_nextId;
            Inserted.Add((id, message, metadataJson));
            return Task.FromResult(id);
        }

        public Task MarkSentAsync(long id, DateTimeOffset sentAtUtc, CancellationToken cancellationToken)
        {
            Sent.Add(id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(long id, string error, DateTimeOffset failedAtUtc, CancellationToken cancellationToken)
        {
            Failed.Add((id, error));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, EmailContent Content)> Sent { get; } = [];

        public Task SendAsync(string to, EmailContent content, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, content));
            return Task.CompletedTask;
        }
    }
}
