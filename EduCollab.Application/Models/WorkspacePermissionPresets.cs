namespace EduCollab.Application.Models
{
    public static class WorkspacePermissionPresets
    {
        public static readonly IReadOnlyList<WorkspacePermissionDefinition> Catalog =
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

        private static readonly IReadOnlyDictionary<string, WorkspacePermissionDefinition> CatalogByKey =
            Catalog.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<WorkspaceRole, IReadOnlySet<string>> RoleTemplates =
            new Dictionary<WorkspaceRole, IReadOnlySet<string>>
            {
                [WorkspaceRole.Owner] = ToSet(
                [
                    "editWorkspace", "seeGroupsTab", "addGroups", "seeUsersTab", "inviteUsers",
                    "addAssets", "addScenes", "addFlows", "createSessions", "loadAssets", "loadScenes",
                    "editorOutliner", "editorSequencer", "editorActions", "editorDetails", "editorConsole",
                    "editorCatalog", "editorUi", "editorTransformTools",
                ]),
                [WorkspaceRole.Manager] = ToSet(
                [
                    "seeGroupsTab", "addGroups", "seeUsersTab", "inviteUsers",
                    "addAssets", "addScenes", "addFlows", "createSessions", "loadAssets", "loadScenes",
                    "editorOutliner", "editorSequencer", "editorActions", "editorDetails", "editorConsole",
                    "editorCatalog", "editorUi", "editorTransformTools",
                ]),
                [WorkspaceRole.Creator] = ToSet(
                [
                    "addAssets", "addScenes", "addFlows", "createSessions", "loadAssets", "loadScenes",
                    "editorOutliner", "editorSequencer", "editorActions", "editorDetails", "editorConsole",
                    "editorCatalog", "editorUi", "editorTransformTools",
                ]),
                [WorkspaceRole.Viewer] = ToSet(["loadScenes"]),
            };

        private static readonly IReadOnlyDictionary<string, WorkspaceRole> RoleShortcutByKey =
            new Dictionary<string, WorkspaceRole>(StringComparer.OrdinalIgnoreCase)
            {
                ["owner"] = WorkspaceRole.Owner,
                ["manager"] = WorkspaceRole.Manager,
                ["creator"] = WorkspaceRole.Creator,
                ["viewer"] = WorkspaceRole.Viewer,
            };

        public static IReadOnlyCollection<string> RoleShortcutKeys { get; } =
            RoleShortcutByKey.Keys.ToArray();

        public static bool TryNormalizeKeys(
            IEnumerable<string>? presetKeys,
            out IReadOnlySet<string> normalized,
            out string? error)
        {
            normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            error = null;

            if (presetKeys is null)
            {
                error = "At least one preset key is required.";
                return false;
            }

            var keys = presetKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .ToList();

            if (keys.Count == 0)
            {
                error = "At least one preset key is required.";
                return false;
            }

            var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                if (RoleShortcutByKey.TryGetValue(key, out var role))
                {
                    foreach (var presetKey in GetPresetKeysForRole(role))
                    {
                        expanded.Add(presetKey);
                    }

                    continue;
                }

                if (!CatalogByKey.ContainsKey(key))
                {
                    error = $"Unknown preset or role shortcut key '{key}'.";
                    return false;
                }

                expanded.Add(key);
            }

            normalized = expanded;
            return true;
        }

        public static WorkspaceRole DeriveRole(IReadOnlySet<string> presetKeys)
        {
            foreach (var (role, template) in RoleTemplates)
            {
                if (presetKeys.SetEquals(template))
                {
                    return role;
                }
            }

            return WorkspaceRole.Custom;
        }

        public static IReadOnlySet<string> GetPresetKeysForRole(WorkspaceRole role)
        {
            if (RoleTemplates.TryGetValue(role, out var template))
            {
                return template;
            }

            return RoleTemplates[WorkspaceRole.Viewer];
        }

        public static IReadOnlySet<string> ResolveMemberPresetKeys(WorkspaceMember member)
        {
            if (member.Presets.Count > 0)
            {
                return member.Presets;
            }

            if (member.Role == WorkspaceRole.Custom)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return GetPresetKeysForRole(member.Role);
        }

        public static WorkspaceRole ResolveMemberRole(WorkspaceMember member) =>
            DeriveRole(ResolveMemberPresetKeys(member));

        public static string ToRoleKey(WorkspaceRole role) =>
            role switch
            {
                WorkspaceRole.Owner => "owner",
                WorkspaceRole.Manager => "manager",
                WorkspaceRole.Creator => "creator",
                WorkspaceRole.Viewer => "viewer",
                _ => "custom",
            };

        public static WorkspacePermissionDefinition? TryGetDefinition(string presetKey) =>
            CatalogByKey.TryGetValue(presetKey, out var definition) ? definition : null;

        private static IReadOnlySet<string> ToSet(IEnumerable<string> keys) =>
            keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
