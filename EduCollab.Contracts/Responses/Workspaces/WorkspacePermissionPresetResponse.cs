namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspacePermissionPresetResponse
    {
        public string Key { get; set; } = string.Empty;

        public List<string> Parameters { get; set; } = new();
    }
}
