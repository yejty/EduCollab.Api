$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$hubTestProject = Join-Path $repoRoot "Space.Api.Tests\Space.Api.Tests.csproj"
$driveTestProject = Join-Path $repoRoot "Drive.Api.Tests\Drive.Api.Tests.csproj"
$hubOutputFile = Join-Path $repoRoot "docs\hub.openapi.json"
$driveHostOutputFile = Join-Path $repoRoot "docs\drive-target.openapi.json"

Push-Location $repoRoot
try {
    $env:EXPORT_OPENAPI = "1"
    $env:ASPNETCORE_ENVIRONMENT = "Testing"

    Write-Host "Exporting Hub OpenAPI document..."
    dotnet test $hubTestProject `
        --filter "FullyQualifiedName=Space.Api.Tests.OpenApiSpecTests.ExportCommittedOpenApiSpec_WhenExportFlagIsSet"

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path $hubOutputFile)) {
        throw "Export did not produce $hubOutputFile"
    }

    Write-Host "Hub OpenAPI spec exported to $hubOutputFile"

    Write-Host "Exporting Drive host OpenAPI document..."
    dotnet test $driveTestProject `
        --filter "FullyQualifiedName=Drive.Api.Tests.OpenApiSpecTests.ExportCommittedOpenApiSpec_WhenExportFlagIsSet"

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path $driveHostOutputFile)) {
        throw "Export did not produce $driveHostOutputFile"
    }

    Write-Host "Drive host OpenAPI spec exported to $driveHostOutputFile"
    Write-Host "Left binding targets untouched: docs/hub-target.openapi.json, docs/drive.openapi.json."
}
finally {
    Remove-Item Env:EXPORT_OPENAPI -ErrorAction SilentlyContinue
    Pop-Location
}
