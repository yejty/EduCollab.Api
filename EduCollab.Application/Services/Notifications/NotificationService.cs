using System.Text.Json;
using EduCollab.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace EduCollab.Application.Services.Notifications
{
    public sealed class NotificationService : INotificationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            IEmailSender emailSender,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (string.IsNullOrWhiteSpace(message.RecipientEmail))
            {
                _logger.LogWarning("Skipped {NotificationType} notification because recipient is empty.", message.Type);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var metadataJson = SerializeMetadata(message);
            var notificationId = await _notificationRepository.InsertPendingAsync(message, metadataJson, now, cancellationToken);

            try
            {
                await _emailSender.SendAsync(message.RecipientEmail, message.Content, cancellationToken);
                await _notificationRepository.MarkSentAsync(notificationId, DateTimeOffset.UtcNow, cancellationToken);
            }
            catch (Exception ex)
            {
                await _notificationRepository.MarkFailedAsync(notificationId, ex.Message, DateTimeOffset.UtcNow, cancellationToken);
                _logger.LogError(
                    ex,
                    "Failed to send {NotificationType} notification {NotificationId} to {RecipientEmail}.",
                    message.Type,
                    notificationId,
                    message.RecipientEmail);
            }
        }

        private static string? SerializeMetadata(NotificationMessage message)
        {
            if ((message.Metadata is null || message.Metadata.Count == 0) &&
                (message.Actions is null || message.Actions.Count == 0) &&
                message.Priority == NotificationPriority.Transactional)
            {
                return null;
            }

            var payload = new
            {
                priority = message.Priority.ToString(),
                actions = message.Actions,
                metadata = message.Metadata
            };

            return JsonSerializer.Serialize(payload, JsonOptions);
        }
    }
}
