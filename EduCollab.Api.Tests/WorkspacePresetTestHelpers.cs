using EduCollab.Application.Models;

namespace EduCollab.Api.Tests;

internal static class WorkspacePresetTestHelpers
{
    public static List<string> PresetsForRole(WorkspaceRole role) =>
        WorkspacePermissionPresets.GetPresetKeysForRole(role).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();
}
