namespace EduCollab.Application.Services.Notifications
{
    public interface INotificationService
    {
        Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    }
}
