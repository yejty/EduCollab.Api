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

        public static bool CanSeeUsersTab(WorkspaceMember member) =>
            HasPreset(member, "seeUsersTab") || HasPreset(member, "inviteUsers");

        public static bool CanInviteUsers(WorkspaceMember member) =>
            HasPreset(member, "inviteUsers");

        public static bool CanManageGroups(WorkspaceMember member) =>
            HasPreset(member, "addGroups");

        public static bool CanCreateAssets(WorkspaceMember member) =>
            HasPreset(member, "addAssets");

        public static bool CanCreateScenesAndFlows(WorkspaceMember member) =>
            HasPreset(member, "addScenesAndFlows")
            || HasPreset(member, "addScenes")
            || HasPreset(member, "addFlows");

        public static bool CanCreateScenes(WorkspaceMember member) =>
            CanCreateScenesAndFlows(member);

        public static bool CanCreateFlows(WorkspaceMember member) =>
            CanCreateScenesAndFlows(member);

        public static bool CanLoadAssets(WorkspaceMember member) =>
            HasPreset(member, "loadAssets") || HasPreset(member, "addAssets");

        public static bool CanLoadScenesAndFlows(WorkspaceMember member) =>
            HasPreset(member, "loadScenesAndFlows")
            || HasPreset(member, "loadScenes")
            || CanCreateScenesAndFlows(member);

        public static bool HasOnlyLoadScenesAndFlowsPreset(WorkspaceMember member)
        {
            var presets = ResolvePresets(member);
            return presets.Count > 0
                && presets.All(IsLoadScenesAndFlowsOnlyPresetKey);
        }

        public static bool CanViewGroupMembers(WorkspaceMember member) =>
            CanSeeUsersTab(member) || CanManageGroups(member);

        public static bool CanManageGroupMembers(WorkspaceMember member) =>
            CanSeeAllContent(member) || CanManageGroups(member);

        public static bool CanViewAssetGroupShares(WorkspaceMember member) =>
            CanCreateAssets(member);

        public static bool CanViewResourceGroupShares(WorkspaceMember member) =>
            CanCreateScenesAndFlows(member);

        public static bool CanManageOthersContentViaGroups(WorkspaceMember member) =>
            HasPreset(member, "addGroups")
            && (HasPreset(member, "addAssets") || CanCreateScenesAndFlows(member));

        private static bool IsLoadScenesAndFlowsOnlyPresetKey(string key) =>
            string.Equals(key, "loadScenesAndFlows", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "loadScenes", StringComparison.OrdinalIgnoreCase);
    }
}
