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
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes" },
        };

        Assert.True(WorkspacePresetPermissions.HasPreset(member, "loadScenes"));
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

        Assert.True(WorkspacePresetPermissions.HasPreset(member, "loadScenes"));
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
        bool canLoadScenes)
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
        Assert.Equal(canLoadScenes, WorkspacePresetPermissions.CanLoadScenes(member));
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
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "addGroups", "addAssets", "loadAssets", "loadScenes" },
        };

        Assert.True(WorkspacePresetPermissions.CanManageOthersContentViaGroups(manager));
        Assert.False(WorkspacePresetPermissions.CanManageOthersContentViaGroups(creator));
        Assert.True(WorkspacePresetPermissions.CanManageOthersContentViaGroups(custom));
    }
}
