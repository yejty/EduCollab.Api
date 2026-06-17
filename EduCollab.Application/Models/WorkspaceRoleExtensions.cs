namespace EduCollab.Application.Models
{
    public static class WorkspaceRoleExtensions
    {
        public static string ToPersistedString(this WorkspaceRole role) => role.ToString();

        public static bool TryFromPersisted(string? value, out WorkspaceRole role)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                role = WorkspaceRole.Viewer;
                return false;
            }

            if (string.Equals(value, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                role = WorkspaceRole.Manager;
                return true;
            }

            if (string.Equals(value, "Member", StringComparison.OrdinalIgnoreCase))
            {
                role = WorkspaceRole.Viewer;
                return true;
            }

            return Enum.TryParse(value, ignoreCase: true, out role);
        }

        public static WorkspaceRole FromPersistedOrViewer(string? value) =>
            TryFromPersisted(value, out var role) ? role : WorkspaceRole.Viewer;
    }
}
