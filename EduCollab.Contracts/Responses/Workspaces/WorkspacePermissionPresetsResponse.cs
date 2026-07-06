namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspacePermissionPresetsResponse
    {
        public List<WorkspacePermissionPresetResponse> Presets { get; set; } = new();
    }
}
