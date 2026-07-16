using System.Globalization;
using EduCollab.Application.Models;

namespace EduCollab.Api.Tests;

public sealed class WorkspacePermissionParametersTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> CsvMatrix = LoadCsvMatrix();

    [Fact]
    public void RoleTemplates_MatchRolesFunctionsCsv()
    {
        var csvParameterKeys = CsvMatrix.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var templateRoleKeys = new[] { "owner", "manager", "creator", "viewer" };
        Assert.Equal(csvParameterKeys, templateRoleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase));

        foreach (var roleKey in templateRoleKeys)
        {
            var role = Enum.Parse<WorkspaceRole>(roleKey, ignoreCase: true);
            var template = WorkspacePermissionParameters.GetParameterKeysForRole(role);
            var csvRow = CsvMatrix[roleKey];

            foreach (var definition in WorkspacePermissionParameters.Catalog)
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

        var catalogKeys = WorkspacePermissionParameters.Catalog
            .Select(p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(permissionKeysFromCsv, catalogKeys);
    }

    [Fact]
    public void DeriveRole_ReturnsCustom_WhenParameterCombinationDoesNotMatchTemplate()
    {
        var customParameters = new[] { "addAssets", "loadScenes", "loadFlows" }.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(WorkspaceRole.Custom, WorkspacePermissionParameters.DeriveRole(customParameters));
    }

    [Theory]
    [InlineData("owner", WorkspaceRole.Owner)]
    [InlineData("manager", WorkspaceRole.Manager)]
    [InlineData("creator", WorkspaceRole.Creator)]
    [InlineData("viewer", WorkspaceRole.Viewer)]
    public void DeriveRole_MatchesRoleTemplates(string roleKey, WorkspaceRole expectedRole)
    {
        var role = Enum.Parse<WorkspaceRole>(roleKey, ignoreCase: true);
        var parameters = WorkspacePermissionParameters.GetParameterKeysForRole(role);

        Assert.Equal(expectedRole, WorkspacePermissionParameters.DeriveRole(parameters));
    }

    [Theory]
    [InlineData("manager", WorkspaceRole.Manager)]
    [InlineData("owner", WorkspaceRole.Owner)]
    [InlineData("creator", WorkspaceRole.Creator)]
    [InlineData("viewer", WorkspaceRole.Viewer)]
    public void TryNormalizeKeys_ExpandsRoleShortcut(string roleShortcut, WorkspaceRole expectedRole)
    {
        Assert.True(WorkspacePermissionParameters.TryNormalizeKeys([roleShortcut], out var normalized, out var error), error);
        Assert.Equal(expectedRole, WorkspacePermissionParameters.DeriveRole(normalized));
        Assert.Equal(
            WorkspacePermissionParameters.GetParameterKeysForRole(expectedRole),
            normalized);
    }

    [Fact]
    public void TryNormalizeKeys_MergesRoleShortcutWithIndividualParameters()
    {
        Assert.True(WorkspacePermissionParameters.TryNormalizeKeys(["viewer", "addAssets"], out var normalized, out _));

        Assert.DoesNotContain("loadScenes", normalized);
        Assert.Contains("loadFlows", normalized);
        Assert.Contains("addAssets", normalized);
        Assert.Equal(WorkspaceRole.Custom, WorkspacePermissionParameters.DeriveRole(normalized));
    }

    [Fact]
    public void TryNormalizeKeys_AcceptsAddScenesAndAddFlowsIndependently()
    {
        Assert.True(WorkspacePermissionParameters.TryNormalizeKeys(["addScenes"], out var fromScenes, out _));
        Assert.Contains("addScenes", fromScenes);
        Assert.DoesNotContain("addFlows", fromScenes);

        Assert.True(WorkspacePermissionParameters.TryNormalizeKeys(["addFlows"], out var fromFlows, out _));
        Assert.Contains("addFlows", fromFlows);
        Assert.DoesNotContain("addScenes", fromFlows);
    }

    [Fact]
    public void ResolveMemberRole_ReturnsViewer_WhenOnlyLoadFlowsIsAssigned()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadFlows" },
        };

        Assert.Equal(WorkspaceRole.Viewer, WorkspacePermissionParameters.ResolveMemberRole(member));
        Assert.Equal("viewer", WorkspacePermissionParameters.ToRoleKey(WorkspacePermissionParameters.ResolveMemberRole(member)));
    }

    [Fact]
    public void ResolveMemberRole_ReturnsCustom_WhenLoadScenesAndLoadFlowsAreAssigned()
    {
        var member = new WorkspaceMember
        {
            Role = WorkspaceRole.Custom,
            Parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "loadScenes", "loadFlows" },
        };

        Assert.Equal(WorkspaceRole.Custom, WorkspacePermissionParameters.ResolveMemberRole(member));
    }

    [Fact]
    public void TryNormalizeKeys_ExpandsLegacyCombinedAliases()
    {
        Assert.True(WorkspacePermissionParameters.TryNormalizeKeys(["loadScenesAndFlows"], out var loadNormalized, out _));
        Assert.Contains("loadScenes", loadNormalized);
        Assert.Contains("loadFlows", loadNormalized);
        Assert.DoesNotContain("loadScenesAndFlows", loadNormalized);

        Assert.True(WorkspacePermissionParameters.TryNormalizeKeys(["addScenesAndFlows"], out var addNormalized, out _));
        Assert.Contains("addScenes", addNormalized);
        Assert.Contains("addFlows", addNormalized);
        Assert.DoesNotContain("addScenesAndFlows", addNormalized);
    }

    [Fact]
    public void TryNormalizeKeys_RejectsUnknownParameter()
    {
        Assert.False(WorkspacePermissionParameters.TryNormalizeKeys(["addAssets", "unknown"], out _, out var error));
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

        var permissionKeyByCsvLabel = WorkspacePermissionParameters.Catalog
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
