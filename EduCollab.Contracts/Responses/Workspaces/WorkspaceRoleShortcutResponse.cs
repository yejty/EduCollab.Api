namespace EduCollab.Contracts.Responses.Workspaces
{
    public sealed class WorkspaceRoleShortcutResponse
    {
        public string Key { get; set; } = string.Empty;

        public List<string> Presets { get; set; } = new();
    }
}
