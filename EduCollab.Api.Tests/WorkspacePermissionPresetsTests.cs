using System.Globalization;
using EduCollab.Application.Models;

namespace EduCollab.Api.Tests;

public sealed class WorkspacePermissionPresetsTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> CsvMatrix = LoadCsvMatrix();

    [Fact]
    public void RoleTemplates_MatchRolesFunctionsCsv()
    {
        var csvPresetKeys = CsvMatrix.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var templateRoleKeys = new[] { "owner", "manager", "creator", "viewer" };
        Assert.Equal(csvPresetKeys, templateRoleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase));

        foreach (var roleKey in templateRoleKeys)
        {
            var role = Enum.Parse<WorkspaceRole>(roleKey, ignoreCase: true);
            var template = WorkspacePermissionPresets.GetPresetKeysForRole(role);
            var csvRow = CsvMatrix[roleKey];

            foreach (var definition in WorkspacePermissionPresets.Catalog)
            {
                Assert.Equal(csvRow[definition.Key], template.Contains(definition.Key));
            }
        }
    }

    [Fact]
    public void Catalog_ContainsAllCsvPermissionRows()
    {
        var permissionKeysFromCsv = CsvMatrix.Values
            .SelectMany(row => row.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var catalogKeys = WorkspacePermissionPresets.Catalog
            .Select(p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(permissionKeysFromCsv, catalogKeys);
    }

    [Fact]
    public void DeriveRole_ReturnsCustom_WhenPresetCombinationDoesNotMatchTemplate()
    {
        var customPresets = new[] { "addAssets", "loadScenesAndFlows" }.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(WorkspaceRole.Custom, WorkspacePermissionPresets.DeriveRole(customPresets));
    }

    [Theory]
    [InlineData("owner", WorkspaceRole.Owner)]
    [InlineData("manager", WorkspaceRole.Manager)]
    [InlineData("creator", WorkspaceRole.Creator)]
    [InlineData("viewer", WorkspaceRole.Viewer)]
    public void DeriveRole_MatchesRoleTemplates(string roleKey, WorkspaceRole expectedRole)
    {
        var role = Enum.Parse<WorkspaceRole>(roleKey, ignoreCase: true);
        var presets = WorkspacePermissionPresets.GetPresetKeysForRole(role);

        Assert.Equal(expectedRole, WorkspacePermissionPresets.DeriveRole(presets));
    }

    [Theory]
    [InlineData("manager", WorkspaceRole.Manager)]
    [InlineData("owner", WorkspaceRole.Owner)]
    [InlineData("creator", WorkspaceRole.Creator)]
    [InlineData("viewer", WorkspaceRole.Viewer)]
    public void TryNormalizeKeys_ExpandsRoleShortcut(string roleShortcut, WorkspaceRole expectedRole)
    {
        Assert.True(WorkspacePermissionPresets.TryNormalizeKeys([roleShortcut], out var normalized, out var error), error);
        Assert.Equal(expectedRole, WorkspacePermissionPresets.DeriveRole(normalized));
        Assert.Equal(
            WorkspacePermissionPresets.GetPresetKeysForRole(expectedRole),
            normalized);
    }

    [Fact]
    public void TryNormalizeKeys_MergesRoleShortcutWithIndividualPresets()
    {
        Assert.True(WorkspacePermissionPresets.TryNormalizeKeys(["viewer", "addAssets"], out var normalized, out _));

        Assert.Contains("loadScenesAndFlows", normalized);
        Assert.Contains("addAssets", normalized);
        Assert.Equal(WorkspaceRole.Custom, WorkspacePermissionPresets.DeriveRole(normalized));
    }

    [Fact]
    public void TryNormalizeKeys_AcceptsLegacyAddScenesAndAddFlowsAliases()
    {
        Assert.True(WorkspacePermissionPresets.TryNormalizeKeys(["addScenes"], out var fromScenes, out _));
        Assert.Contains("addScenesAndFlows", fromScenes);
        Assert.DoesNotContain("addScenes", fromScenes);

        Assert.True(WorkspacePermissionPresets.TryNormalizeKeys(["addFlows"], out var fromFlows, out _));
        Assert.Contains("addScenesAndFlows", fromFlows);
        Assert.DoesNotContain("addFlows", fromFlows);
    }

    [Fact]
    public void ResolveMemberRole_ReturnsViewer_WhenOnlyLoadScenesAndFlowsPresetIsAssigned()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Presets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenesAndFlows" },
        };

        Assert.Equal(WorkspaceRole.Viewer, WorkspacePermissionPresets.ResolveMemberRole(member));
        Assert.Equal("viewer", WorkspacePermissionPresets.ToRoleKey(WorkspacePermissionPresets.ResolveMemberRole(member)));
    }

    [Fact]
    public void TryNormalizeKeys_AcceptsLegacyLoadScenesAlias()
    {
        Assert.True(WorkspacePermissionPresets.TryNormalizeKeys(["loadScenes"], out var normalized, out _));
        Assert.Contains("loadScenesAndFlows", normalized);
        Assert.DoesNotContain("loadScenes", normalized);
    }

    [Fact]
    public void TryNormalizeKeys_RejectsUnknownPreset()
    {
        Assert.False(WorkspacePermissionPresets.TryNormalizeKeys(["addAssets", "unknown"], out _, out var error));
        Assert.Contains("unknown", error, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> LoadCsvMatrix()
    {
        var csvPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Helpers", "Roles-Functions.csv"));

        Assert.True(File.Exists(csvPath), $"CSV not found at {csvPath}");

        string[] lines;
        using (var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
        {
            var content = reader.ReadToEnd();
            lines = content.Split(["\r\n", "\n"], StringSplitOptions.None);
        }

        var header = lines[0].Split(';');
        var presetColumns = header.Skip(1).ToArray();

        var permissionKeyByCsvLabel = WorkspacePermissionPresets.Catalog
            .ToDictionary(p => NormalizeLabel(p.Label), p => p.Key, StringComparer.OrdinalIgnoreCase);

        var matrix = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in presetColumns)
        {
            matrix[column] = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(';');
            var label = parts[0].Trim();
            var permissionKey = permissionKeyByCsvLabel[NormalizeLabel(label)];

            for (var i = 0; i < presetColumns.Length; i++)
            {
                var granted = string.Equals(parts[i + 1].Trim(), "TRUE", StringComparison.OrdinalIgnoreCase);
                matrix[presetColumns[i]][permissionKey] = granted;
            }
        }

        return matrix.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyDictionary<string, bool>)kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeLabel(string label) =>
        label.Trim().ToLower(CultureInfo.InvariantCulture);
}
