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
                "- **Groups** `GET /api/workspace/groups`: optional `accessible=true` and `parentGroupId` to browse the group tree. Root browse (`parentGroupId` omitted) returns entry groups the caller may open (direct membership plus subgroups whose parent is not accessible). Drill down with `parentGroupId={groupId}`.",
                "- **Group library** `GET /api/workspace/groups/{groupId}/assets|scenes|flows`: resources placed in that group (requires effective group access).",
                "- **Assets** `GET /api/workspace/assets`: union of all accessible assets. Optional filter: `owner=me`. Each asset has a single `groupId` placement. Binary content is a ZIP file via `PUT /api/workspace/assets/{assetId}/content`.",
                "- **Scenes** `GET /api/workspace/scenes`: optional `owner=me`. Each scene has a single `groupId` placement.",
                "- **Flows** `GET /api/workspace/flows`: optional `owner=me`. Each flow has a single `groupId` placement.",
                "- **Scene assets** `GET /api/workspace/scene-assets`: required `sceneId` (scene-context asset list). Attach via `POST /api/workspace/scene-assets`; detach via `DELETE /api/workspace/scene-assets?sceneId=&assetId=`.",
                "- **Flow scenes** `GET /api/workspace/flow-scenes`: required `flowId`. Link via `POST /api/workspace/flow-scenes`; unlink via `DELETE /api/workspace/flow-scenes?flowId=&sceneId=`.",
                "- **Asset moves** `POST /api/workspace/asset-moves`: body `{ assetId, groupId }` to move an asset to another group.",
                "",
                "## Errors",
                "All `4xx` and `5xx` responses use RFC 9457 Problem Details (`application/problem+json`).",
                "Common properties: `type`, `title`, `status`, `detail`, `instance`, `error`, `requestId`.",
                "Model validation failures use `error: validation_failed` and an `errors` object keyed by field name (camelCase).",
                "Every response includes an `X-Request-Id` header matching `requestId` in error bodies when present.",
                "",
                "## Contract source",
                "This document is generated from the running API (Swashbuckle). The committed copy lives at `openapi/v1/openapi.json`.",
                "Regenerate with `scripts/export-openapi.ps1`.");
        }
    }
}
