[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$directory = New-Item -ItemType Directory -Path $OutputDirectory -Force
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputPath = Join-Path $directory.FullName "LibrarySystem-migrations-$timestamp.sql"

Push-Location $repositoryRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Restoring local .NET tools failed.' }

    dotnet tool run dotnet-ef migrations script --idempotent `
        --project LibrarySystem.Infrastructure\LibrarySystem.Infrastructure.csproj `
        --startup-project LibrarySystem\LibrarySystem.csproj `
        --output $outputPath
    if ($LASTEXITCODE -ne 0) { throw 'Generating the migration script failed.' }
}
finally {
    Pop-Location
}

Write-Output "Generated migration artifact: $outputPath"
