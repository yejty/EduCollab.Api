namespace EduCollab.Application.Models
{
    public enum WorkspaceCreationAdminReviewAction
    {
        Approve,
        Deny,
    }

    public static class WorkspaceCreationAdminReviewActionExtensions
    {
        public static string ToPersistedString(this WorkspaceCreationAdminReviewAction action) =>
            action switch
            {
                WorkspaceCreationAdminReviewAction.Approve => "Approve",
                WorkspaceCreationAdminReviewAction.Deny => "Deny",
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
            };

        public static WorkspaceCreationAdminReviewAction FromPersisted(string value) =>
            value switch
            {
                "Approve" => WorkspaceCreationAdminReviewAction.Approve,
                "Deny" => WorkspaceCreationAdminReviewAction.Deny,
                _ => throw new ArgumentException($"Unknown admin review action: {value}", nameof(value)),
            };
    }
}
