using EduCollab.Application.Models;



namespace EduCollab.Api.Tests;



public sealed class WorkspacePresetPermissionsTests

{

    [Fact]

    public void HasPreset_UsesAssignedPresets_WhenMemberHasCustomCombination()

    {

        var member = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenesAndFlows" },

        };



        Assert.True(WorkspacePresetPermissions.HasPreset(member, "loadScenesAndFlows"));

        Assert.False(WorkspacePresetPermissions.HasPreset(member, "loadAssets"));

        Assert.False(WorkspacePresetPermissions.CanCreateAssets(member));

    }



    [Fact]

    public void HasPreset_FallsBackToRoleTemplate_WhenPresetsAreEmpty()

    {

        var member = new WorkspaceMember

        {

            Role = WorkspaceRole.Viewer,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase),

        };



        Assert.True(WorkspacePresetPermissions.HasPreset(member, "loadScenesAndFlows"));

        Assert.False(WorkspacePresetPermissions.CanLoadAssets(member));

    }



    [Theory]

    [InlineData(WorkspaceRole.Owner, true, true, true, true)]

    [InlineData(WorkspaceRole.Manager, false, true, true, true)]

    [InlineData(WorkspaceRole.Creator, false, false, true, true)]

    [InlineData(WorkspaceRole.Viewer, false, false, false, true)]

    public void StandardRoleTemplates_MatchCsvGates(

        WorkspaceRole role,

        bool canEditWorkspace,

        bool canManageGroups,

        bool canLoadAssets,

        bool canLoadScenesAndFlows)

    {

        var member = new WorkspaceMember

        {

            Role = role,

            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(role),

        };



        Assert.Equal(canEditWorkspace, WorkspacePresetPermissions.CanManageWorkspace(member));

        Assert.Equal(canEditWorkspace, WorkspacePresetPermissions.CanSeeAllContent(member));

        Assert.Equal(canManageGroups, WorkspacePresetPermissions.CanManageGroups(member));

        Assert.Equal(canLoadAssets, WorkspacePresetPermissions.CanLoadAssets(member));

        Assert.Equal(canLoadScenesAndFlows, WorkspacePresetPermissions.CanLoadScenesAndFlows(member));

    }



    [Fact]

    public void CanManageOthersContentViaGroups_RequiresAddGroupsAndContentPreset()

    {

        var manager = new WorkspaceMember

        {

            Role = WorkspaceRole.Manager,

            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(WorkspaceRole.Manager),

        };

        var creator = new WorkspaceMember

        {

            Role = WorkspaceRole.Creator,

            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(WorkspaceRole.Creator),

        };

        var custom = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "addAssets", "loadAssets", "loadScenesAndFlows" },

        };



        Assert.True(WorkspacePresetPermissions.CanManageOthersContentViaGroups(manager));

        Assert.False(WorkspacePresetPermissions.CanManageOthersContentViaGroups(creator));

        Assert.True(WorkspacePresetPermissions.CanManageOthersContentViaGroups(custom));

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

            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(role),

        };



        Assert.Equal(expected, WorkspacePresetPermissions.CanSeeUsersTab(member));

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

            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(role),

        };



        Assert.Equal(expected, WorkspacePresetPermissions.CanViewGroupMembers(member));
    }

    [Fact]
    public void CanManageGroupMembers_RequiresOwnerOrAddGroups()
    {
        var owner = new WorkspaceMember
        {
            Role = WorkspaceRole.Owner,
            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(WorkspaceRole.Owner),
        };
        var addGroupsOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "loadScenesAndFlows" },
        };
        var creator = new WorkspaceMember
        {
            Role = WorkspaceRole.Creator,
            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(WorkspaceRole.Creator),
        };

        Assert.True(WorkspacePresetPermissions.CanManageGroupMembers(owner));
        Assert.True(WorkspacePresetPermissions.CanManageGroupMembers(addGroupsOnly));
        Assert.False(WorkspacePresetPermissions.CanManageGroupMembers(creator));
    }

    [Fact]
    public void CanViewGroupMembers_IsTrue_WhenMemberHasAddGroupsWithoutSeeUsersTab()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "loadScenesAndFlows" },
        };

        Assert.False(WorkspacePresetPermissions.CanSeeUsersTab(member));
        Assert.True(WorkspacePresetPermissions.CanManageGroups(member));
        Assert.True(WorkspacePresetPermissions.CanViewGroupMembers(member));
    }



    [Fact]

    public void HasOnlyLoadScenesAndFlowsPreset_IsTrue_WhenLoadPresetIsTheOnlyPreset()

    {

        var member = new WorkspaceMember

        {

            Role = WorkspaceRole.Viewer,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenesAndFlows" },

        };



        Assert.True(WorkspacePresetPermissions.HasOnlyLoadScenesAndFlowsPreset(member));

        Assert.False(WorkspacePresetPermissions.CanViewGroupMembers(member));

        Assert.False(WorkspacePresetPermissions.CanViewResourceGroupShares(member));

    }



    [Fact]

    public void CanViewGroupMembers_IsFalse_WhenMemberLacksSeeUsersTab()

    {

        var member = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenesAndFlows", "addAssets" },

        };



        Assert.False(WorkspacePresetPermissions.HasOnlyLoadScenesAndFlowsPreset(member));

        Assert.False(WorkspacePresetPermissions.CanViewGroupMembers(member));

        Assert.False(WorkspacePresetPermissions.CanViewResourceGroupShares(member));

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

            Presets = WorkspacePermissionPresets.GetPresetKeysForRole(role),

        };



        Assert.Equal(expected, WorkspacePresetPermissions.CanViewAssetGroupShares(member));

    }



    [Fact]

    public void CanViewAssetGroupShares_IsFalse_WhenMemberHasLoadAssetsWithoutAddAssets()

    {

        var member = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadAssets", "loadScenesAndFlows" },

        };



        Assert.True(WorkspacePresetPermissions.CanLoadAssets(member));

        Assert.False(WorkspacePresetPermissions.CanViewAssetGroupShares(member));

    }



    [Fact]

    public void AddAssets_ImplicitlyGrantsLoadAssetsPermission()

    {

        var loadOnly = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadAssets" },

        };

        var addOnly = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addAssets" },

        };



        Assert.True(WorkspacePresetPermissions.CanLoadAssets(loadOnly));

        Assert.False(WorkspacePresetPermissions.CanCreateAssets(loadOnly));

        Assert.False(WorkspacePresetPermissions.HasPreset(loadOnly, "addAssets"));



        Assert.True(WorkspacePresetPermissions.CanLoadAssets(addOnly));

        Assert.True(WorkspacePresetPermissions.CanCreateAssets(addOnly));

        Assert.False(WorkspacePresetPermissions.HasPreset(addOnly, "loadAssets"));

    }



    [Fact]
    public void AddScenesAndFlows_ImplicitlyGrantsLoadScenesAndFlowsPermission()
    {
        var loadOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenesAndFlows" },
        };
        var addOnly = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addScenesAndFlows" },
        };

        Assert.True(WorkspacePresetPermissions.CanLoadScenesAndFlows(loadOnly));
        Assert.False(WorkspacePresetPermissions.CanCreateScenesAndFlows(loadOnly));

        Assert.True(WorkspacePresetPermissions.CanLoadScenesAndFlows(addOnly));
        Assert.True(WorkspacePresetPermissions.CanCreateScenes(addOnly));
        Assert.True(WorkspacePresetPermissions.CanCreateFlows(addOnly));
        Assert.False(WorkspacePresetPermissions.HasPreset(addOnly, "loadScenesAndFlows"));
    }

    [Fact]
    public void LegacyAddScenesAndAddFlowsPresets_AreNormalizedToAddScenesAndFlows()
    {
        var fromScenes = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addScenes" },
        };
        var fromFlows = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addFlows" },
        };

        Assert.True(WorkspacePresetPermissions.CanCreateScenesAndFlows(fromScenes));
        Assert.True(WorkspacePresetPermissions.CanCreateScenesAndFlows(fromFlows));
        Assert.True(WorkspacePresetPermissions.HasPreset(fromScenes, "addScenesAndFlows"));
        Assert.True(WorkspacePresetPermissions.HasPreset(fromFlows, "addScenesAndFlows"));
        Assert.False(WorkspacePresetPermissions.HasPreset(fromScenes, "addScenes"));
        Assert.False(WorkspacePresetPermissions.HasPreset(fromFlows, "addFlows"));
    }



    [Fact]

    public void InviteUsers_ImplicitlyGrantsSeeUsersTabPermission()

    {

        var seeOnly = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "seeUsersTab" },

        };

        var inviteOnly = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "inviteUsers" },

        };



        Assert.True(WorkspacePresetPermissions.CanSeeUsersTab(seeOnly));

        Assert.False(WorkspacePresetPermissions.CanInviteUsers(seeOnly));

        Assert.False(WorkspacePresetPermissions.HasPreset(seeOnly, "inviteUsers"));



        Assert.True(WorkspacePresetPermissions.CanSeeUsersTab(inviteOnly));

        Assert.True(WorkspacePresetPermissions.CanInviteUsers(inviteOnly));

        Assert.False(WorkspacePresetPermissions.HasPreset(inviteOnly, "seeUsersTab"));

        Assert.True(WorkspacePresetPermissions.CanViewGroupMembers(inviteOnly));

    }



    [Fact]

    public void LegacyLoadScenesPreset_IsNormalizedToLoadScenesAndFlows()

    {

        var member = new WorkspaceMember

        {

            Role = WorkspaceRole.Custom,

            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes" },

        };



        Assert.False(WorkspacePresetPermissions.HasPreset(member, "loadScenes"));

        Assert.True(WorkspacePresetPermissions.HasPreset(member, "loadScenesAndFlows"));

        Assert.True(WorkspacePresetPermissions.CanLoadScenesAndFlows(member));

        Assert.Equal(WorkspaceRole.Viewer, WorkspacePermissionPresets.ResolveMemberRole(member));

    }

}


