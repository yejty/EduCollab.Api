namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspacePermissionPresetResponse
    {
        public string Key { get; set; } = string.Empty;

        public bool IsRole { get; set; }

        public List<WorkspacePermissionItemResponse> Permissions { get; set; } = new();
    }
}
