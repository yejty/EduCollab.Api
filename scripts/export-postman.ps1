$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "Space.Api.Tests\Space.Api.Tests.csproj"
$outputFile = Join-Path $repoRoot "Space.Api.postman_collection.json"
$environmentFile = Join-Path $repoRoot "Space.Api.postman_environment.json"

Push-Location $repoRoot
try {
    Write-Host "Exporting Postman collection from openapi/v1/hub.openapi.json ..."
    $env:EXPORT_POSTMAN = "1"

    dotnet test $testProject `
        --filter "FullyQualifiedName=Space.Api.Tests.PostmanCollectionTests.ExportCommittedPostmanCollection_WhenExportFlagIsSet"

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path $outputFile)) {
        throw "Export did not produce $outputFile"
    }

    if (-not (Test-Path $environmentFile)) {
        throw "Export did not produce $environmentFile"
    }

    Write-Host "Postman collection exported to $outputFile"
    Write-Host "Postman environment exported to $environmentFile"
}
finally {
    Remove-Item Env:EXPORT_POSTMAN -ErrorAction SilentlyContinue
    Pop-Location
}
