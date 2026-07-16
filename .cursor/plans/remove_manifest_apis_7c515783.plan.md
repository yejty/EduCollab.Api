---
name: Flow-scoped manifests
overview: Restore contextual manifests. Per-group includeAssets on scene/flow shares. Viewers default+migrate to loadFlows only. Slice 1: scene-assets + scene includeAssets + download tokens (contextual only). Slice 2: flow-scenes + flowId on scene-assets. Peer/owner paths use inline JSON and direct asset content (no tokens).
todos:
  - id: scene-share-include-assets
    content: "Slice 1: IncludeAssets on SceneGroupShares; per-group attach/set; restore SceneAssetsController; tokens only for contextual includeAssets downloads"
    status: completed
  - id: download-tokens
    content: "Slice 1: Mint 10m reusable download tokens on contextual manifest rows; /content requires login JWT + download token"
    status: completed
  - id: viewer-default-loadflows
    content: Viewer template = loadFlows only; migrate existing members who only had the old viewer loadScenes+loadFlows set
    status: completed
  - id: flow-share-include-assets
    content: "Slice 2: IncludeAssets on FlowGroupShares; per-group attach/set"
    status: pending
  - id: restore-flow-scenes
    content: "Slice 2: flow-scenes manifest + tokenized content; scene-assets?flowId= contextual auth"
    status: pending
  - id: flow-sceneids-crud
    content: "Slice 2: sceneIds on Flow CRUD; ≥1 scene; caller access required; always return sceneIds"
    status: pending
  - id: permission-orthogonality
    content: Enforce loadScenes vs loadFlows independently; document frontend tab gating
    status: pending
  - id: update-tests
    content: Tests for scene includeAssets + tokens, peer skip-token path, viewer migration, then flow-scoped path
    status: completed
  - id: update-docs
    content: Update OpenApiContractDescriptions, openapi.json, Postman
    status: pending
isProject: false
---

# Flow-scoped Manifests (decisions locked)

## Decision

**Do not remove manifests.** Restore / add contextual manifests.

Colyseus / live sessions: **out of scope for this work** (defer).

### Locked answers

| # | Decision |
|---|----------|
| 1 | Live `sceneIds`; UI prompts on flow update for dependencies. |
| 2 | List **200** + `includeAssets: false` message; `/content` **403** when not included. |
| 3 | `includeAssets` **per group**. |
| 4 | ≥1 accessible `sceneId` on flow create/update. |
| 5 | Include-assets on **scene** and **flow** shares. |
| 6 | `GET /flows/{id}` always returns `sceneIds`. |
| 7 | Viewer default = **`loadFlows` only**; **migrate existing** viewers. |
| 8 | Slice 1 = scene-assets; slice 2 = flow-scenes; no Colyseus. |
| 9 | No `flow-assets`; per-scene `scene-assets` (+ `flowId` in slice 2). |
| 10 | Download tokens: **10 min**, **reusable until exp**, renew by re-fetching manifest. |
| 11 | `/content` requires **login JWT + download token** (both). |
| 12 | `loadScenes`: `GET /scenes/{id}` returns **`jsonContent` inline** (not tokenized). |
| 13 | Peer `loadAssets` / direct asset visibility: use **`GET /assets/{id}/content`** — **skip tokens**. |
| 14 | Replace shares body: `{ "groups": [ { "groupId", "includeAssets" } ] }` OK. |
| 15 | **Owner/author path:** if you created the flow and can already access the attached scenes (they were shared/accessible to you), use **normal scene/asset access — no download tokens**. Tokens are for **contextual recipients** only (`includeAssets` without peer library access). |

---

## Slice 1 status (done)

Implemented:

- `SceneGroupShares.IncludeAssets` + migration
- Per-group share APIs (`AttachSceneGroupRequest.IncludeAssets`, `SetSceneGroupsRequest.Groups`)
- `GET /scene-assets` + tokenized `GET /scene-assets/content?token=`
- `ContentDownloadTokenService` (10 min, reusable, dual JWT)
- Viewer template + CSV = `loadFlows` only; migrate exact old viewer param set
- Unit + integration tests added (run against local Postgres `:5433`)

## Remaining: Slice 2

- Flow `IncludeAssets` on `FlowGroupShares`
- `flow-scenes` + optional `flowId` on `scene-assets`
- Flow CRUD `sceneIds` (≥1) if not already complete
- Docs / OpenAPI / Postman
