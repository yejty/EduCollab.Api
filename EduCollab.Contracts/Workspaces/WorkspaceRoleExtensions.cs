namespace EduCollab.Contracts.Workspaces
{
    public static class WorkspaceRoleExtensions
    {
        public static string ToPersistedString(this WorkspaceRole role) => role.ToString();

        public static bool TryFromPersisted(string? value, out WorkspaceRole role) =>
            Enum.TryParse(value, ignoreCase: true, out role);

        public static WorkspaceRole FromPersistedOrMember(string? value) =>
            TryFromPersisted(value, out var r) ? r : WorkspaceRole.Member;
    }
}
