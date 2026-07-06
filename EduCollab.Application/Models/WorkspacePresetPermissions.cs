namespace EduCollab.Application.Models
{
    public static class WorkspacePresetPermissions
    {
        public static IReadOnlySet<string> ResolvePresets(WorkspaceMember member) =>
            WorkspacePermissionPresets.ResolveMemberPresetKeys(member);

        public static bool HasPreset(WorkspaceMember member, string key) =>
            ResolvePresets(member).Contains(key);

        public static bool HasAnyPreset(WorkspaceMember member, params string[] keys)
        {
            var presets = ResolvePresets(member);
            foreach (var key in keys)
            {
                if (presets.Contains(key))
                    return true;
            }

            return false;
        }

        public static bool CanSeeAllContent(WorkspaceMember member) =>
            HasPreset(member, "editWorkspace");

        public static bool CanManageWorkspace(WorkspaceMember member) =>
            HasPreset(member, "editWorkspace");

        public static bool CanInviteUsers(WorkspaceMember member) =>
            HasPreset(member, "inviteUsers");

        public static bool CanManageGroups(WorkspaceMember member) =>
            HasPreset(member, "addGroups");

        public static bool CanCreateAssets(WorkspaceMember member) =>
            HasPreset(member, "addAssets");

        public static bool CanCreateScenes(WorkspaceMember member) =>
            HasPreset(member, "addScenes");

        public static bool CanCreateFlows(WorkspaceMember member) =>
            HasPreset(member, "addFlows");

        public static bool CanLoadAssets(WorkspaceMember member) =>
            HasPreset(member, "loadAssets");

        public static bool CanLoadScenes(WorkspaceMember member) =>
            HasPreset(member, "loadScenes");

        public static bool CanManageOthersContentViaGroups(WorkspaceMember member) =>
            HasPreset(member, "addGroups")
            && HasAnyPreset(member, "addAssets", "addScenes", "addFlows");
    }
}
