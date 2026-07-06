using System.Globalization;
using EduCollab.Application.Models;

namespace EduCollab.Api.Tests;

public sealed class WorkspacePermissionPresetsTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> CsvMatrix = LoadCsvMatrix();

    [Fact]
    public void AllPresets_MatchRolesFunctionsCsv()
    {
        var csvPresetKeys = CsvMatrix.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var registryPresetKeys = WorkspacePermissionPresets.AllPresets
            .Select(p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(csvPresetKeys, registryPresetKeys);

        foreach (var preset in WorkspacePermissionPresets.AllPresets)
        {
            Assert.True(preset.IsRole);

            var csvRow = CsvMatrix[preset.Key];
            foreach (var permission in WorkspacePermissionPresets.AllPermissions)
            {
                var expected = csvRow[permission.Key];
                var actual = preset.GrantedPermissionKeys.Contains(permission.Key);
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void AllPermissions_AreDefinedInRegistry()
    {
        var permissionKeysFromCsv = CsvMatrix.Values
            .SelectMany(row => row.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var registryKeys = WorkspacePermissionPresets.AllPermissions
            .Select(p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(permissionKeysFromCsv, registryKeys);
    }

    [Theory]
    [InlineData("owner", WorkspaceRole.Owner)]
    [InlineData("manager", WorkspaceRole.Manager)]
    [InlineData("creator", WorkspaceRole.Creator)]
    [InlineData("viewer", WorkspaceRole.Viewer)]
    public void TryToWorkspaceRole_MapsPresetToRole(string presetKey, WorkspaceRole expectedRole)
    {
        Assert.True(WorkspacePermissionPresets.TryToWorkspaceRole(presetKey, out var role));
        Assert.Equal(expectedRole, role);
    }

    [Fact]
    public void FromWorkspaceRole_RoundTripsPresetKey()
    {
        foreach (var role in new[] { WorkspaceRole.Owner, WorkspaceRole.Manager, WorkspaceRole.Creator, WorkspaceRole.Viewer })
        {
            var preset = WorkspacePermissionPresets.FromWorkspaceRole(role);
            Assert.Equal(role, preset.Role);
            Assert.Equal(preset.Key, WorkspacePermissionPresets.ToPresetKey(role));
        }
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

        var permissionKeyByCsvLabel = WorkspacePermissionPresets.AllPermissions
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
