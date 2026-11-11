using Space.Application.Models;

namespace Space.Api.Tests;

internal static class WorkspaceParameterTestHelpers
{
    public static List<string> ParametersForRole(WorkspaceRole role) =>
        WorkspacePermissionParameters.GetParameterKeysForRole(role).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();
}
