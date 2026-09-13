[CmdletBinding()]
param(
    [string]$Project = 'LibrarySystem/LibrarySystem.csproj',
    [string]$OutputDirectory = 'artifacts/releases',
    [string]$Version = (Get-Date -Format 'yyyyMMdd-HHmmss')
)

$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^[A-Za-z0-9._-]+$') { throw 'Version may contain only letters, numbers, dots, underscores, and hyphens.' }
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Project))
$releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$publishDirectory = Join-Path $releaseRoot "LibrarySystem-$Version"
$archivePath = "$publishDirectory.zip"
$manifestPath = "$archivePath.manifest.json"
if ((Test-Path -LiteralPath $publishDirectory) -or (Test-Path -LiteralPath $archivePath)) {
    throw "Release artifact $Version already exists; choose a unique version."
}

New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
dotnet publish $projectPath --configuration Release --output $publishDirectory --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }
Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal
$archive = Get-Item -LiteralPath $archivePath
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
$commit = (git -C $repositoryRoot rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0) { $commit = 'unavailable' }

[ordered]@{
    application = 'LibrarySystem'
    version = $Version
    generatedUtc = [DateTime]::UtcNow.ToString('O')
    gitCommit = $commit
    archiveFile = $archive.Name
    archiveBytes = $archive.Length
    sha256 = $hash
    targetFramework = 'net9.0'
} | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Output "Artifact: $archivePath"
Write-Output "Manifest: $manifestPath"
Write-Output "SHA256: $hash"
