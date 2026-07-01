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
                "- **Workspace / group members**: `userId`, `joinedAt`, `role` where applicable (default `joinedAt` ascending).",
                "- **Workspace creation requests**: `name`, `createdAt`, `status`, `id` (default `createdAt` descending).",
                "",
                "Paged list responses include `page`, `pageSize`, and `totalCount` alongside the collection.",
                "",
                "### Collection filters (normalized URLs)",
                "- **Groups** `GET /api/workspace/groups`: tree browse with optional `parentId`. Root browse (`parentId` omitted) returns entry groups the caller may open (direct membership plus subgroups whose parent is not accessible). Drill down with `parentId={groupId}`. `GET /api/workspace/groups/flat` returns all accessible groups in one list (group pickers, search, admin tables). Adding a user to a group also adds them to all subgroups.",
                "- **Group library** `GET /api/workspace/groups/{groupId}/assets|scenes|flows`: resources placed in that group (requires effective group access).",
                "- **Assets** `GET /api/workspace/assets`: union of all accessible assets. Optional filter: `owner=me`. Each asset has group placement via `groupId` / `groupIds`; change placement with `PUT /api/workspace/assets/{assetId}`. Binary content is a ZIP file via `PUT /api/workspace/assets/{assetId}/content`.",
                "- **Scenes** `GET /api/workspace/scenes`: union of all accessible scenes. Optional filter: `owner=me`. CRUD by id at `/api/workspace/scenes/{sceneId}`. Create/update send inline `jsonContent` as JSON or upload a `.json` file via `multipart/form-data` (`jsonFile` part).",
                "- **Flows** `GET /api/workspace/flows`: union of all accessible flows. Optional filter: `owner=me`. CRUD by id at `/api/workspace/flows/{flowId}`.",
                "- **Scene assets** `GET /api/workspace/scene-assets`: required `sceneId` (authoritative manifest of assets required to render the scene). Attach via `POST /api/workspace/scene-assets`; detach via `DELETE /api/workspace/scene-assets?sceneId=&assetId=`. Download ZIP content in scene context via `GET /api/workspace/scene-assets/content?sceneId=&assetId=` (required when `canViewDirectly` is false).",
                "- **Flow scenes** `GET /api/workspace/flow-scenes`: required `flowId` (authoritative manifest of scenes in the flow). Attach via `POST /api/workspace/flow-scenes`; detach via `DELETE /api/workspace/flow-scenes?flowId=&sceneId=`. Load scene JSON in flow context via `GET /api/workspace/flow-scenes/content?flowId=&sceneId=` (required when `canViewDirectly` is false).",
                "",
                "## Scene runtime asset loading",
                "Clients rendering a scene should:",
                "1. `GET /api/workspace/scenes/{sceneId}` — load `jsonContent` (object graph; objects use `assetId` to point at workspace assets).",
                "2. `GET /api/workspace/scene-assets?sceneId=` — server-resolved manifest with `usableInScene` and `canViewDirectly` flags.",
                "3. Download each required asset ZIP:",
                "   - When `canViewDirectly` is true: `GET /api/workspace/assets/{assetId}/content`.",
                "   - When `canViewDirectly` is false but `usableInScene` is true: `GET /api/workspace/scene-assets/content?sceneId=&assetId=` (scene-context only).",
                "4. Cross-check every `assetId` parsed from `jsonContent` against the manifest; ids missing from the manifest are broken or inaccessible references.",
                "",
                "Scene-context access allows runtime use of assets referenced by a visible scene without granting standalone asset library access.",
                "",
                "## Flow runtime scene loading",
                "Clients running a flow should:",
                "1. `GET /api/workspace/flows/{flowId}` — load flow metadata.",
                "2. `GET /api/workspace/flow-scenes?flowId=` — server-resolved manifest with `usableInFlow` and `canViewDirectly` flags.",
                "3. Load each attached scene JSON:",
                "   - When `canViewDirectly` is true: `GET /api/workspace/scenes/{sceneId}`.",
                "   - When `canViewDirectly` is false but `usableInFlow` is true: `GET /api/workspace/flow-scenes/content?flowId=&sceneId=` (flow-context only).",
                "",
                "Flow-context access allows runtime use of scenes attached to a visible flow without granting standalone scene library access.",
                "",
                "## Errors",
                "All `4xx` and `5xx` responses use RFC 9457 Problem Details (`application/problem+json`).",
                "Common properties: `type`, `title`, `status`, `detail`, `instance`, `error`, `requestId`.",
                "Model validation failures use `error: validation_failed` and an `errors` object keyed by field name (camelCase).",
                "Invalid scene asset references on create/update use `error: invalid_asset_reference` with an `invalidAssetReferences` array of `{ assetId, reason }`.",
                "Every response includes an `X-Request-Id` header matching `requestId` in error bodies when present.",
                "",
                "## Contract source",
                "This document is generated from the running API (Swashbuckle). The committed copy lives at `openapi/v1/openapi.json`.",
                "Regenerate with `scripts/export-openapi.ps1`.");
        }
    }
}
