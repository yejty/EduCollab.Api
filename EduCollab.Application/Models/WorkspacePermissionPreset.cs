namespace EduCollab.Application.Models
{
    public sealed class WorkspacePermissionPreset
    {
        public required string Key { get; init; }

        public required bool IsRole { get; init; }

        public required WorkspaceRole Role { get; init; }

        public required IReadOnlySet<string> GrantedPermissionKeys { get; init; }
    }
}
