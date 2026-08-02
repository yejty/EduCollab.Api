namespace EduCollab.Application.Models
{
    public static class WorkspaceParameterPermissions
    {
        public static IReadOnlySet<string> ResolveParameters(WorkspaceMember member) =>
            WorkspacePermissionParameters.ResolveMemberParameterKeys(member);

        public static bool HasParameter(WorkspaceMember member, string key) =>
            ResolveParameters(member).Contains(key);

        public static bool HasAnyParameter(WorkspaceMember member, params string[] keys)
        {
            var parameters = ResolveParameters(member);
            foreach (var key in keys)
            {
                if (parameters.Contains(key))
                    return true;
            }

            return false;
        }

        public static bool CanSeeAllContent(WorkspaceMember member) =>
            HasParameter(member, "editWorkspace");

        public static bool CanManageWorkspace(WorkspaceMember member) =>
            HasParameter(member, "editWorkspace");

        public static bool CanSeeUsersTab(WorkspaceMember member) =>
            HasParameter(member, "seeUsersTab") || HasParameter(member, "inviteUsers");

        public static bool CanInviteUsers(WorkspaceMember member) =>
            HasParameter(member, "inviteUsers");

        public static bool CanManageGroups(WorkspaceMember member) =>
            HasParameter(member, "addGroups");

        public static bool CanCreateAssets(WorkspaceMember member) =>
            HasParameter(member, "addAssets");

        public static bool CanCreateScenes(WorkspaceMember member) =>
            HasParameter(member, "addScenes");

        public static bool CanCreateFlows(WorkspaceMember member) =>
            HasParameter(member, "addFlows");

        public static bool CanCreateSessions(WorkspaceMember member) =>
            HasParameter(member, "createSessions");

        public static bool CanLoadAssets(WorkspaceMember member) =>
            HasParameter(member, "loadAssets") || HasParameter(member, "addAssets");

        public static bool CanLoadScenes(WorkspaceMember member) =>
            HasParameter(member, "loadScenes") || CanCreateScenes(member);

        public static bool CanLoadFlows(WorkspaceMember member) =>
            HasParameter(member, "loadFlows") || CanCreateFlows(member);

        public static bool HasOnlyLoadScenesAndFlowsParameter(WorkspaceMember member)
        {
            var parameters = ResolveParameters(member);
            return parameters.Count > 0
                && parameters.All(IsLoadScenesOrFlowsOnlyParameterKey);
        }

        public static bool CanViewGroupMembers(WorkspaceMember member) =>
            CanSeeUsersTab(member) || CanManageGroups(member);

        public static bool CanManageGroupMembers(WorkspaceMember member) =>
            CanSeeAllContent(member) || CanManageGroups(member);

        public static bool CanViewAssetGroupShares(WorkspaceMember member) =>
            CanCreateAssets(member);

        public static bool CanViewSceneGroupShares(WorkspaceMember member) =>
            CanCreateScenes(member);

        public static bool CanViewFlowGroupShares(WorkspaceMember member) =>
            CanCreateFlows(member);

        public static bool CanManageOthersContentViaGroups(WorkspaceMember member) =>
            HasParameter(member, "addGroups")
            && (HasParameter(member, "addAssets") || CanCreateScenes(member) || CanCreateFlows(member));

        private static bool IsLoadScenesOrFlowsOnlyParameterKey(string key) =>
            string.Equals(key, "loadScenes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "loadFlows", StringComparison.OrdinalIgnoreCase);
    }
}
