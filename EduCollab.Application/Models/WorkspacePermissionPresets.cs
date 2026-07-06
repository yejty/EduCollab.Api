namespace EduCollab.Application.Models
{
    public static class WorkspacePermissionPresets
    {
        public static readonly IReadOnlyList<WorkspacePermissionDefinition> AllPermissions =
        [
            new() { Key = "editWorkspace", Label = "Edit workspace" },
            new() { Key = "seeGroupsTab", Label = "See groups tab" },
            new() { Key = "addGroups", Label = "Add groups" },
            new() { Key = "seeUsersTab", Label = "See users tab" },
            new() { Key = "inviteUsers", Label = "Invite users" },
            new() { Key = "addAssets", Label = "Add assets" },
            new() { Key = "addScenes", Label = "Add scenes" },
            new() { Key = "addFlows", Label = "Add flows" },
            new() { Key = "createSessions", Label = "Create sessions" },
            new() { Key = "loadAssets", Label = "Load assets" },
            new() { Key = "loadScenes", Label = "Load scenes" },
            new() { Key = "editorOutliner", Label = "Editor / outliner panel" },
            new() { Key = "editorSequencer", Label = "Editor / sequencer panel" },
            new() { Key = "editorActions", Label = "Editor / actions panel" },
            new() { Key = "editorDetails", Label = "Editor / details panel" },
            new() { Key = "editorConsole", Label = "Editor / console panel" },
            new() { Key = "editorCatalog", Label = "Editor / catalog panel" },
            new() { Key = "editorUi", Label = "Editor / UI editor" },
            new() { Key = "editorTransformTools", Label = "Editor / transform tools" },
        ];

        private static readonly IReadOnlyDictionary<string, WorkspacePermissionDefinition> PermissionsByKey =
            AllPermissions.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        public static readonly IReadOnlyList<WorkspacePermissionPreset> AllPresets =
        [
            CreatePreset("owner", WorkspaceRole.Owner,
            [
                "editWorkspace", "seeGroupsTab", "addGroups", "seeUsersTab", "inviteUsers",
                "addAssets", "addScenes", "addFlows", "createSessions", "loadAssets", "loadScenes",
                "editorOutliner", "editorSequencer", "editorActions", "editorDetails", "editorConsole",
                "editorCatalog", "editorUi", "editorTransformTools",
            ]),
            CreatePreset("manager", WorkspaceRole.Manager,
            [
                "seeGroupsTab", "addGroups", "seeUsersTab", "inviteUsers",
                "addAssets", "addScenes", "addFlows", "createSessions", "loadAssets", "loadScenes",
                "editorOutliner", "editorSequencer", "editorActions", "editorDetails", "editorConsole",
                "editorCatalog", "editorUi", "editorTransformTools",
            ]),
            CreatePreset("creator", WorkspaceRole.Creator,
            [
                "addAssets", "addScenes", "addFlows", "createSessions", "loadAssets", "loadScenes",
                "editorOutliner", "editorSequencer", "editorActions", "editorDetails", "editorConsole",
                "editorCatalog", "editorUi", "editorTransformTools",
            ]),
            CreatePreset("viewer", WorkspaceRole.Viewer,
            [
                "loadScenes",
            ]),
        ];

        private static readonly IReadOnlyDictionary<string, WorkspacePermissionPreset> PresetsByKey =
            AllPresets.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<WorkspaceRole, WorkspacePermissionPreset> PresetsByRole =
            AllPresets.ToDictionary(p => p.Role);

        public static IReadOnlyCollection<string> AllPresetKeys { get; } =
            AllPresets.Select(p => p.Key).ToArray();

        public static bool TryFromKey(string? presetKey, out WorkspacePermissionPreset preset)
        {
            if (string.IsNullOrWhiteSpace(presetKey))
            {
                preset = null!;
                return false;
            }

            return PresetsByKey.TryGetValue(presetKey.Trim(), out preset!);
        }

        public static bool TryToWorkspaceRole(string? presetKey, out WorkspaceRole role)
        {
            if (TryFromKey(presetKey, out var preset))
            {
                role = preset.Role;
                return true;
            }

            role = WorkspaceRole.Viewer;
            return false;
        }

        public static WorkspacePermissionPreset FromWorkspaceRole(WorkspaceRole role)
        {
            if (PresetsByRole.TryGetValue(role, out var preset))
            {
                return preset;
            }

            return PresetsByRole[WorkspaceRole.Viewer];
        }

        public static string ToPresetKey(WorkspaceRole role) => FromWorkspaceRole(role).Key;

        public static bool IsGranted(string presetKey, string permissionKey)
        {
            if (!TryFromKey(presetKey, out var preset))
            {
                return false;
            }

            return preset.GrantedPermissionKeys.Contains(permissionKey);
        }

        public static WorkspacePermissionDefinition? TryGetPermission(string permissionKey)
        {
            return PermissionsByKey.TryGetValue(permissionKey, out var permission) ? permission : null;
        }

        private static WorkspacePermissionPreset CreatePreset(
            string key,
            WorkspaceRole role,
            IEnumerable<string> grantedPermissionKeys)
        {
            return new WorkspacePermissionPreset
            {
                Key = key,
                IsRole = true,
                Role = role,
                GrantedPermissionKeys = grantedPermissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase),
            };
        }
    }
}
