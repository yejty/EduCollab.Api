# Helpers

Design assets and reference material for Space (not part of the API runtime).

| Path | Purpose |
|------|---------|
| `design-tokens.md` / `index.css` | Email and UI token reference (used by `SpaceEmailTemplates`) |
| `*.png` | Diagrams and mockups |

**API contract:** use [`openapi/v1/hub.openapi.json`](../openapi/v1/hub.openapi.json) (Hub host), [`openapi/v1/drive-host.openapi.json`](../openapi/v1/drive-host.openapi.json) (Drive host), and [`Space.Api.postman_collection.json`](../Space.Api.postman_collection.json), not this folder.

Regenerate those with `scripts/export-openapi.ps1` and `scripts/export-postman.ps1`.
