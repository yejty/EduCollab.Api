namespace EduCollab.Application.Models
{
    public enum WorkspaceCreationRequestStatus
    {
        Pending,
        Approved,
        Denied,
    }

    public static class WorkspaceCreationRequestStatusExtensions
    {
        public static string ToPersistedString(this WorkspaceCreationRequestStatus status) => status switch
        {
            WorkspaceCreationRequestStatus.Pending => "Pending",
            WorkspaceCreationRequestStatus.Approved => "Approved",
            WorkspaceCreationRequestStatus.Denied => "Denied",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        public static WorkspaceCreationRequestStatus FromPersisted(string value) => value switch
        {
            "Pending" => WorkspaceCreationRequestStatus.Pending,
            "Approved" => WorkspaceCreationRequestStatus.Approved,
            "Denied" => WorkspaceCreationRequestStatus.Denied,
            _ => WorkspaceCreationRequestStatus.Pending,
        };
    }
}
