using EduCollab.Application.Services.Notifications;

namespace EduCollab.Application.Repositories
{
    public interface INotificationRepository
    {
        Task<long> InsertPendingAsync(
            NotificationMessage message,
            string? metadataJson,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken);

        Task MarkSentAsync(long id, DateTimeOffset sentAtUtc, CancellationToken cancellationToken);

        Task MarkFailedAsync(long id, string error, DateTimeOffset failedAtUtc, CancellationToken cancellationToken);
    }
}
