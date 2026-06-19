namespace EduCollab.Application.Services.Notifications
{
    public enum NotificationType
    {
        EmailConfirmation,
        PasswordReset,
        LoginCode,
        WorkspaceInvitation,
        WorkspaceCreationRequestSubmitted,
        WorkspaceCreationApproved,
        WorkspaceCreationDenied,
        ProfileUpdated,
        PasswordChanged
    }

    public enum NotificationPriority
    {
        Transactional,
        Normal,
        Low
    }

    public enum NotificationStatus
    {
        Pending,
        Sent,
        Failed,
        Skipped
    }

    public enum NotificationActionStyle
    {
        Primary,
        Secondary,
        Danger
    }

    public sealed record NotificationAction(
        string Label,
        string Url,
        NotificationActionStyle Style = NotificationActionStyle.Primary,
        string? ExpiresIn = null);

    public sealed record NotificationMessage(
        string RecipientEmail,
        NotificationType Type,
        EmailContent Content,
        NotificationPriority Priority = NotificationPriority.Transactional,
        IReadOnlyList<NotificationAction>? Actions = null,
        IReadOnlyDictionary<string, string>? Metadata = null)
    {
        public static NotificationMessage Create(
            string recipientEmail,
            NotificationType type,
            EmailContent content,
            NotificationPriority priority = NotificationPriority.Transactional,
            IReadOnlyList<NotificationAction>? actions = null,
            IReadOnlyDictionary<string, string>? metadata = null) =>
            new(recipientEmail, type, content, priority, actions, metadata);
    }
}
