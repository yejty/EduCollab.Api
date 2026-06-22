# Export OpenAPI specification

Generates `openapi/v1/openapi.json` from the running API using the integration test host (same document as `/swagger/v1/swagger.json`).

## Prerequisites

- .NET 8 SDK

## Usage

From the repository root:

```powershell
.\scripts\export-openapi.ps1
```

The script runs a dedicated export test that writes `openapi/v1/openapi.json`.

After changing controllers, DTOs, or Swagger filters, re-run the script and commit the updated JSON. Unit test `OpenApiSpecTests.CommittedOpenApiSpec_matchesLiveSwaggerDocument` fails when the committed file is stale.

## Postman

Regenerate [`EduCollab.Api.postman_collection.json`](../../EduCollab.Api.postman_collection.json) from this spec:

```powershell
.\scripts\export-postman.ps1
```

Set collection variables `baseUrl` and `accessToken` after import. Contract test `PostmanCollectionTests.CommittedPostmanCollection_matchesOpenApiExport` fails when the collection is stale.

## Live document

When the API is running, the same document is served at:

- `/swagger/v1/swagger.json`
- Swagger UI at `/swagger`
