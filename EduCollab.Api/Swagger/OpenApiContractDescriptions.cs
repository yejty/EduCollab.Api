using EduCollab.Api.Query;

namespace EduCollab.Api.Swagger
{
    public static class OpenApiContractDescriptions
    {
        public const string DocumentName = "v1";

        public const string Title = "EduCollab API";

        public const string Version = "1.0";

        public static string BuildInfoDescription()
        {
            return string.Join('\n',
                "EduCollab workspace collaboration API (v1).",
                "",
                "## Base URL and tenancy",
                "- All routes are under `/api`.",
                "- Authenticated workspace operations use the **current workspace** scope: `/api/workspace/...`.",
                "- Users may belong to multiple workspaces; `Users.WorkspaceId` stores the active workspace used by current-workspace routes.",
                "- List memberships with `GET /api/users/me/workspaces`; switch active workspace with `PUT /api/users/me/active-workspace`.",
                "- Platform administration uses `/api/admin/...`.",
                "",
                "## Authentication",
                "- Send `Authorization: Bearer {accessToken}` on protected routes.",
                "- Obtain tokens from `POST /api/users/login`, `POST /api/users/login/confirm-code`, or `POST /api/users/registration-confirm`.",
                "",
                "## Health",
                "- `GET /api/health` returns API and dependency status without authentication.",
                "- Returns `200` when healthy and `503` when a dependency (e.g. database) is unavailable.",
                "",
                "## Workspace permission parameters",
                "- Workspace-scoped routes under `/api/workspace/...` require the caller to be a member of the **active workspace** (`Users.WorkspaceId`).",
                "- Beyond membership, many routes require one or more **permission parameters** assigned to the member (their preset).",
                "- List parameter keys, labels, and named presets with `GET /api/workspace/permission-parameters`.",
                "- Each workspace operation documents required parameters in its description (**Workspace parameters:** …).",
                "- Parameter requirements also appear in the machine-readable `x-workspace-parameters` OpenAPI extension.",
                "- Members with `editWorkspace` (workspace owners) bypass most parameter checks and can access all workspace content.",
                "- A workspace may have any number of owners, managers, and custom-parameter members; the last owner cannot leave, be removed, or be demoted.",
                "- Content visibility (assets, scenes, flows, groups) may additionally require group membership or ownership even when load parameters are present.",
                "- `POST /api/workspace/invitations` requires `groupId`: the invitee joins that group on accept and gains access to its content and subgroups. Inviter may only assign parameters they hold and may only target a group they can access.",
                "",
                "## List query parameters",
                "Collection list endpoints accept optional query parameters:",
                "",
                "| Parameter | Default | Rules |",
                "|-----------|---------|-------|",
                $"| `page` | {PaginationDefaults.DefaultPage} | Must be >= 1. Invalid values return `400` with `error: invalid_pagination`. |",
                $"| `pageSize` | {PaginationDefaults.DefaultPageSize} | Must be 1–{PaginationDefaults.MaxPageSize}. Invalid values return `400` with `error: invalid_pagination`. |",
                "| `sort` | per resource (see below) | Ascending: `sort=name`. Descending: `sort=-createdAt`. Unknown fields return `400` with `error: invalid_sort`. |",
                "",
                "### Allowed `sort` fields by resource",
                "- **Assets, scenes, groups, flows, admin workspaces**: `name`, `createdAt`, `updatedAt`, `id` (default `name` ascending).",
                "- **Workspace / group members**: `userId`, `email`, `joinedAt`, `role` where applicable (default `joinedAt` ascending).",
                "- **Workspace creation requests**: `name`, `createdAt`, `status`, `id` (default `createdAt` descending).",
                "",
                "Paged list responses include `page`, `pageSize`, and `totalCount` alongside the collection.",
                "",
                "### Collection filters (normalized URLs)",
                "- **Groups** `GET /api/workspace/groups`: tree browse with optional `parentId`. Root browse (`parentId` omitted) returns entry groups the caller may open (direct membership plus subgroups whose parent is not accessible). Drill down with `parentId={groupId}`. `GET /api/workspace/groups/flat` returns all accessible groups in one list (group pickers, search, admin tables). Adding a user to a group also adds them to all subgroups.",
                "- **Group library** `GET /api/workspace/groups/{groupId}/assets|scenes|flows`: resources placed in that group (requires effective group access).",
                "- **Assets** `GET /api/workspace/assets`: union of all accessible assets. Optional filter: `owner=me`. Each asset has group placement via `groupId` / `groupIds`; change placement with `PUT /api/workspace/asset-groups?assetId={assetId}`. Binary content is a ZIP file via `PUT /api/workspace/assets/{assetId}/content`.",
                "- **Scenes** `GET /api/workspace/scenes`: union of all accessible scenes. Optional filter: `owner=me`. CRUD by id at `/api/workspace/scenes/{sceneId}`. Change group placement with `PUT /api/workspace/scene-groups?sceneId={sceneId}`. Create/update send inline `jsonContent` as JSON or upload a `.json` file via `multipart/form-data` (`jsonFile` part); omit `groupIds` to keep the scene in your personal space. Scene JSON may reference workspace assets via `assetId` properties anywhere in the tree.",
                "- **Flows** `GET /api/workspace/flows`: union of all accessible flows. Optional filter: `owner=me`. CRUD by id at `/api/workspace/flows/{flowId}`. Change group placement with `PUT /api/workspace/flow-groups?flowId={flowId}`. Create/update accept optional `groupIds` (omit for personal space) and optional `sceneIds` to attach scenes to the flow.",
                "",
                "## Scene runtime asset loading",
                "Clients rendering a scene should:",
                "1. `GET /api/workspace/scenes/{sceneId}` — load `jsonContent` when the scene itself is visible.",
                "2. Parse `jsonContent` client-side for `assetId` properties.",
                "3. Download each required asset ZIP via `GET /api/workspace/assets/{assetId}/content`. Assets the caller cannot access directly return `404`.",
                "",
                "## Flow runtime scene loading",
                "Clients running a flow should:",
                "1. `GET /api/workspace/flows/{flowId}` — load flow metadata and `sceneIds` when the flow itself is visible.",
                "2. `GET /api/workspace/scenes/{sceneId}` for each `sceneId` (scenes the caller cannot access return `404`).",
                "3. Parse each scene's `jsonContent` for `assetId` values and download assets via `GET /api/workspace/assets/{assetId}/content`.",
                "",
                "## Errors",
                "All `4xx` and `5xx` responses use RFC 9457 Problem Details (`application/problem+json`).",
                "Common properties: `type`, `title`, `status`, `detail`, `instance`, `error`, `requestId`.",
                "Model validation failures use `error: validation_failed` and an `errors` object keyed by field name (camelCase).",
                "Invalid scene asset references on create/update use `error: invalid_asset_reference` with an `invalidAssetReferences` array of `{ assetId, reason }`.",
                "Invalid flow scene references on create/update use `error: invalid_scene_reference` with an `invalidSceneReferences` array of `{ sceneId, reason }`.",
                "Every response includes an `X-Request-Id` header matching `requestId` in error bodies when present.",
                "",
                "## Contract source",
                "This document is generated from the running API (Swashbuckle). The committed copy lives at `openapi/v1/openapi.json`.",
                "Regenerate with `scripts/export-openapi.ps1`.");
        }
    }
}
