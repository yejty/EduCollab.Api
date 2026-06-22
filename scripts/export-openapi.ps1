$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "EduCollab.Api.Tests\EduCollab.Api.Tests.csproj"
$outputFile = Join-Path $repoRoot "openapi\v1\openapi.json"

Push-Location $repoRoot
try {
    Write-Host "Exporting OpenAPI document via test host..."
    $env:EXPORT_OPENAPI = "1"
    $env:ASPNETCORE_ENVIRONMENT = "Testing"

    dotnet test $testProject `
        --filter "FullyQualifiedName=EduCollab.Api.Tests.OpenApiSpecTests.ExportCommittedOpenApiSpec_WhenExportFlagIsSet" `
        --no-restore

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path $outputFile)) {
        throw "Export did not produce $outputFile"
    }

    Write-Host "OpenAPI spec exported to $outputFile"
}
finally {
    Remove-Item Env:EXPORT_OPENAPI -ErrorAction SilentlyContinue
    Pop-Location
}
