# Docs

API contracts and domain references for Hub / Drive. All OpenAPI JSON and the PDF intent live in this folder.

## Start here

| Doc | Purpose |
|-----|---------|
| [api-contracts.md](api-contracts.md) | Frozen hierarchy: PDF intent vs binding targets vs host exports; how to regenerate |

## Files in this folder

### Domain intent (not binding)

| File | Role |
|------|------|
| [`Drive-API-contracts 2.pdf`](Drive-API-contracts%202.pdf) | Product intent: people vs files, roles, flows |
| [`Drive-API-contracts.openapi.json`](Drive-API-contracts.openapi.json) | Machine-readable PDF transcript |

### Binding targets (implement `/api/v1` against these)

| File | Role |
|------|------|
| [`hub-target.openapi.json`](hub-target.openapi.json) | Hub binding `/api/v1` (api-design resource URLs). Reshaped ops document prior paths via `Replaces:` + `x-legacy-paths`. |
| [`drive.openapi.json`](drive.openapi.json) | Drive: storage, content, dispatches only (no token issuance) |

### Exported host reality (may lag targets)

| File | Role |
|------|------|
| [`hub.openapi.json`](hub.openapi.json) | Current Hub.Api Swagger export |
| [`drive-target.openapi.json`](drive-target.openapi.json) | Parked Drive.Api Swagger export (legacy assets/scenes/flows) |

## Quick regenerate

From repo root (requires .NET 10 SDK):

```powershell
.\scripts\export-openapi.ps1   # writes hub.openapi.json + drive-target.openapi.json
.\scripts\export-postman.ps1   # Postman from hub.openapi.json
```

Export never overwrites `hub-target.openapi.json` or `drive.openapi.json`.
