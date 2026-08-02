using EduCollab.Application.Models;

namespace EduCollab.Api.Tests;

public sealed class WorkspaceParameterPermissionsTests
{
    [Fact]
    public void HasParameter_UsesAssignedParameters_WhenMemberHasCustomCombination()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes", "loadFlows" },
        };

        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "loadScenes"));
        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "loadFlows"));
        Assert.False(WorkspaceParameterPermissions.HasParameter(member, "loadAssets"));
        Assert.False(WorkspaceParameterPermissions.CanCreateAssets(member));
    }

    [Fact]
    public void HasParameter_FallsBackToRoleTemplate_WhenParametersAreEmpty()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Viewer,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        };

        Assert.False(WorkspaceParameterPermissions.HasParameter(member, "loadScenes"));
        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "loadFlows"));
        Assert.False(WorkspaceParameterPermissions.CanLoadAssets(member));
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true, true, true, true, true)]
    [InlineData(WorkspaceRole.Manager, false, true, true, true, true)]
    [InlineData(WorkspaceRole.Creator, false, false, true, true, true)]
    [InlineData(WorkspaceRole.Viewer, false, false, false, false, true)]
    public void StandardRoleTemplates_MatchCsvGates(
        WorkspaceRole role,
        bool canEditWorkspace,
        bool canManageGroups,
        bool canLoadAssets,
        bool canLoadScenes,
        bool canLoadFlows)
    {
        var member = new WorkspaceMember
        {
            Role = role,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(role),
        };

        Assert.Equal(canEditWorkspace, WorkspaceParameterPermissions.CanManageWorkspace(member));
        Assert.Equal(canEditWorkspace, WorkspaceParameterPermissions.CanSeeAllContent(member));
        Assert.Equal(canManageGroups, WorkspaceParameterPermissions.CanManageGroups(member));
        Assert.Equal(canLoadAssets, WorkspaceParameterPermissions.CanLoadAssets(member));
        Assert.Equal(canLoadScenes, WorkspaceParameterPermissions.CanLoadScenes(member));
        Assert.Equal(canLoadFlows, WorkspaceParameterPermissions.CanLoadFlows(member));
    }

    [Fact]
    public void CanManageOthersContentViaGroups_RequiresAddGroupsAndContentPreset()
    {
        var manager = new WorkspaceMember
        {
            Role = WorkspaceRole.Manager,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(WorkspaceRole.Manager),
        };
        var creator = new WorkspaceMember
        {
            Role = WorkspaceRole.Creator,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(WorkspaceRole.Creator),
        };
        var custom = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "addAssets", "loadAssets", "loadScenes", "loadFlows" },
        };

        Assert.True(WorkspaceParameterPermissions.CanManageOthersContentViaGroups(manager));
        Assert.False(WorkspaceParameterPermissions.CanManageOthersContentViaGroups(creator));
        Assert.True(WorkspaceParameterPermissions.CanManageOthersContentViaGroups(custom));
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true)]
    [InlineData(WorkspaceRole.Manager, true)]
    [InlineData(WorkspaceRole.Creator, false)]
    [InlineData(WorkspaceRole.Viewer, false)]
    public void CanSeeUsersTab_MatchesCsv(WorkspaceRole role, bool expected)
    {
        var member = new WorkspaceMember
        {
            Role = role,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(role),
        };

        Assert.Equal(expected, WorkspaceParameterPermissions.CanSeeUsersTab(member));
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true)]
    [InlineData(WorkspaceRole.Manager, true)]
    [InlineData(WorkspaceRole.Creator, false)]
    [InlineData(WorkspaceRole.Viewer, false)]
    public void CanViewGroupMembers_MatchesStandardRoleTemplates(WorkspaceRole role, bool expected)
    {
        var member = new WorkspaceMember
        {
            Role = role,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(role),
        };

        Assert.Equal(expected, WorkspaceParameterPermissions.CanViewGroupMembers(member));
    }

    [Fact]
    public void CanManageGroupMembers_RequiresOwnerOrAddGroups()
    {
        var owner = new WorkspaceMember
        {
            Role = WorkspaceRole.Owner,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(WorkspaceRole.Owner),
        };
        var addGroupsOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "loadScenes", "loadFlows" },
        };
        var creator = new WorkspaceMember
        {
            Role = WorkspaceRole.Creator,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(WorkspaceRole.Creator),
        };

        Assert.True(WorkspaceParameterPermissions.CanManageGroupMembers(owner));
        Assert.True(WorkspaceParameterPermissions.CanManageGroupMembers(addGroupsOnly));
        Assert.False(WorkspaceParameterPermissions.CanManageGroupMembers(creator));
    }

    [Fact]
    public void CanViewGroupMembers_IsTrue_WhenMemberHasAddGroupsWithoutSeeUsersTab()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "loadScenes", "loadFlows" },
        };

        Assert.False(WorkspaceParameterPermissions.CanSeeUsersTab(member));
        Assert.True(WorkspaceParameterPermissions.CanManageGroups(member));
        Assert.True(WorkspaceParameterPermissions.CanViewGroupMembers(member));
    }

    [Fact]
    public void HasOnlyLoadScenesAndFlowsParameter_IsTrue_WhenLoadPresetIsTheOnlyPreset()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Viewer,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes", "loadFlows" },
        };

        Assert.True(WorkspaceParameterPermissions.HasOnlyLoadScenesAndFlowsParameter(member));
        Assert.False(WorkspaceParameterPermissions.CanViewGroupMembers(member));
        Assert.False(WorkspaceParameterPermissions.CanViewSceneGroupShares(member));
        Assert.False(WorkspaceParameterPermissions.CanViewFlowGroupShares(member));
    }

    [Fact]
    public void CanViewGroupMembers_IsFalse_WhenMemberLacksSeeUsersTab()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes", "loadFlows", "addAssets" },
        };

        Assert.False(WorkspaceParameterPermissions.HasOnlyLoadScenesAndFlowsParameter(member));
        Assert.False(WorkspaceParameterPermissions.CanViewGroupMembers(member));
        Assert.False(WorkspaceParameterPermissions.CanViewSceneGroupShares(member));
        Assert.False(WorkspaceParameterPermissions.CanViewFlowGroupShares(member));
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true)]
    [InlineData(WorkspaceRole.Manager, true)]
    [InlineData(WorkspaceRole.Creator, true)]
    [InlineData(WorkspaceRole.Viewer, false)]
    public void CanViewAssetGroupShares_RequiresAddAssets(WorkspaceRole role, bool expected)
    {
        var member = new WorkspaceMember
        {
            Role = role,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(role),
        };

        Assert.Equal(expected, WorkspaceParameterPermissions.CanViewAssetGroupShares(member));
    }

    [Fact]
    public void CanViewAssetGroupShares_IsFalse_WhenMemberHasLoadAssetsWithoutAddAssets()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadAssets", "loadScenes", "loadFlows" },
        };

        Assert.True(WorkspaceParameterPermissions.CanLoadAssets(member));
        Assert.False(WorkspaceParameterPermissions.CanViewAssetGroupShares(member));
    }

    [Fact]
    public void AddAssets_ImplicitlyGrantsLoadAssetsPermission()
    {
        var loadOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadAssets" },
        };
        var addOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addAssets" },
        };

        Assert.True(WorkspaceParameterPermissions.CanLoadAssets(loadOnly));
        Assert.False(WorkspaceParameterPermissions.CanCreateAssets(loadOnly));
        Assert.False(WorkspaceParameterPermissions.HasParameter(loadOnly, "addAssets"));

        Assert.True(WorkspaceParameterPermissions.CanLoadAssets(addOnly));
        Assert.True(WorkspaceParameterPermissions.CanCreateAssets(addOnly));
        Assert.False(WorkspaceParameterPermissions.HasParameter(addOnly, "loadAssets"));
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner, true)]
    [InlineData(WorkspaceRole.Manager, true)]
    [InlineData(WorkspaceRole.Creator, true)]
    [InlineData(WorkspaceRole.Viewer, false)]
    public void CanCreateSessions_MatchesRoleTemplate(WorkspaceRole role, bool expected)
    {
        var member = new WorkspaceMember
        {
            Role = role,
            Parameters = WorkspacePermissionParameters.GetParameterKeysForRole(role),
        };

        Assert.Equal(expected, WorkspaceParameterPermissions.CanCreateSessions(member));
    }

    [Fact]
    public void AddScenes_ImplicitlyGrantsLoadScenesPermission()
    {
        var loadOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes" },
        };
        var addOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addScenes" },
        };

        Assert.True(WorkspaceParameterPermissions.CanLoadScenes(loadOnly));
        Assert.False(WorkspaceParameterPermissions.CanCreateScenes(loadOnly));
        Assert.False(WorkspaceParameterPermissions.CanLoadFlows(loadOnly));

        Assert.True(WorkspaceParameterPermissions.CanLoadScenes(addOnly));
        Assert.True(WorkspaceParameterPermissions.CanCreateScenes(addOnly));
        Assert.False(WorkspaceParameterPermissions.HasParameter(addOnly, "loadScenes"));
        Assert.False(WorkspaceParameterPermissions.CanCreateFlows(addOnly));
        Assert.False(WorkspaceParameterPermissions.CanLoadFlows(addOnly));
    }

    [Fact]
    public void AddFlows_ImplicitlyGrantsLoadFlowsPermission()
    {
        var loadOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadFlows" },
        };
        var addOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addFlows" },
        };

        Assert.True(WorkspaceParameterPermissions.CanLoadFlows(loadOnly));
        Assert.False(WorkspaceParameterPermissions.CanCreateFlows(loadOnly));
        Assert.False(WorkspaceParameterPermissions.CanLoadScenes(loadOnly));

        Assert.True(WorkspaceParameterPermissions.CanLoadFlows(addOnly));
        Assert.True(WorkspaceParameterPermissions.CanCreateFlows(addOnly));
        Assert.False(WorkspaceParameterPermissions.HasParameter(addOnly, "loadFlows"));
        Assert.False(WorkspaceParameterPermissions.CanCreateScenes(addOnly));
        Assert.False(WorkspaceParameterPermissions.CanLoadScenes(addOnly));
    }

    [Fact]
    public void LegacyAddScenesAndFlowsParameter_IsNormalizedToAddScenesAndAddFlows()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addScenesAndFlows" },
        };

        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "addScenes"));
        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "addFlows"));
        Assert.False(WorkspaceParameterPermissions.HasParameter(member, "addScenesAndFlows"));
        Assert.True(WorkspaceParameterPermissions.CanCreateScenes(member));
        Assert.True(WorkspaceParameterPermissions.CanCreateFlows(member));
        Assert.True(WorkspaceParameterPermissions.CanLoadScenes(member));
        Assert.True(WorkspaceParameterPermissions.CanLoadFlows(member));
    }

    [Fact]
    public void InviteUsers_ImplicitlyGrantsSeeUsersTabPermission()
    {
        var seeOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "seeUsersTab" },
        };
        var inviteOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "inviteUsers" },
        };

        Assert.True(WorkspaceParameterPermissions.CanSeeUsersTab(seeOnly));
        Assert.False(WorkspaceParameterPermissions.CanInviteUsers(seeOnly));
        Assert.False(WorkspaceParameterPermissions.HasParameter(seeOnly, "inviteUsers"));

        Assert.True(WorkspaceParameterPermissions.CanSeeUsersTab(inviteOnly));
        Assert.True(WorkspaceParameterPermissions.CanInviteUsers(inviteOnly));
        Assert.False(WorkspaceParameterPermissions.HasParameter(inviteOnly, "seeUsersTab"));
        Assert.True(WorkspaceParameterPermissions.CanViewGroupMembers(inviteOnly));
    }

    [Fact]
    public void LegacyLoadScenesAndFlowsParameter_IsNormalizedToLoadScenesAndLoadFlows()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenesAndFlows" },
        };

        Assert.False(WorkspaceParameterPermissions.HasParameter(member, "loadScenesAndFlows"));
        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "loadScenes"));
        Assert.True(WorkspaceParameterPermissions.HasParameter(member, "loadFlows"));
        Assert.True(WorkspaceParameterPermissions.CanLoadScenes(member));
        Assert.True(WorkspaceParameterPermissions.CanLoadFlows(member));
        Assert.Equal(WorkspaceRole.Custom, WorkspacePermissionParameters.ResolveMemberRole(member));
    }
}
