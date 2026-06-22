$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "EduCollab.Api.Tests\EduCollab.Api.Tests.csproj"
$outputFile = Join-Path $repoRoot "EduCollab.Api.postman_collection.json"

Push-Location $repoRoot
try {
    Write-Host "Exporting Postman collection from openapi/v1/openapi.json ..."
    $env:EXPORT_POSTMAN = "1"

    dotnet test $testProject `
        --filter "FullyQualifiedName=EduCollab.Api.Tests.PostmanCollectionTests.ExportCommittedPostmanCollection_WhenExportFlagIsSet" `
        --no-restore

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path $outputFile)) {
        throw "Export did not produce $outputFile"
    }

    Write-Host "Postman collection exported to $outputFile"
}
finally {
    Remove-Item Env:EXPORT_POSTMAN -ErrorAction SilentlyContinue
    Pop-Location
}
