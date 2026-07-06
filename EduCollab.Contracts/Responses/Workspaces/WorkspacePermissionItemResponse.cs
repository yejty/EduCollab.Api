namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspacePermissionItemResponse
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public bool Granted { get; set; }
    }
}
