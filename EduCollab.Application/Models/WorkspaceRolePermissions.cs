namespace EduCollab.Application.Models
{
    public static class WorkspaceRolePermissions
    {
        public static bool CanSeeAllContent(WorkspaceRole role) => role == WorkspaceRole.Owner;

        public static bool CanManageWorkspace(WorkspaceRole role) => role == WorkspaceRole.Owner;

        public static bool CanInviteUsers(WorkspaceRole role) =>
            role is WorkspaceRole.Owner or WorkspaceRole.Manager;

        public static bool CanManageGroups(WorkspaceRole role) =>
            role is WorkspaceRole.Owner or WorkspaceRole.Manager;

        public static bool CanCrudAssets(WorkspaceRole role) =>
            role is WorkspaceRole.Owner or WorkspaceRole.Manager or WorkspaceRole.Creator;

        public static bool CanCrudSharedContent(WorkspaceRole role) =>
            role is WorkspaceRole.Owner or WorkspaceRole.Manager;

        public static bool CanManageAssetFolders(WorkspaceRole role) => role == WorkspaceRole.Owner;

        public static bool CanShareContent(WorkspaceRole role) =>
            role is WorkspaceRole.Owner or WorkspaceRole.Manager;

        public static bool IsReadOnly(WorkspaceRole role) => role == WorkspaceRole.Viewer;
    }
}
