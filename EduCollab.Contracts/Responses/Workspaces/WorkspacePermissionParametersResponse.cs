namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspacePermissionParametersResponse
    {
        public List<WorkspacePermissionParameterResponse> Parameters { get; set; } = new();

        public List<WorkspacePermissionPresetResponse> Presets { get; set; } = new();
    }
}
