[CmdletBinding()]
param(
    [string]$Solution = 'LibrarySystem.sln',
    [string]$OutputPath = 'artifacts/compliance/dependency-licenses.md'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Solution))
$outputFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
New-Item -ItemType Directory -Path (Split-Path -Parent $outputFullPath) -Force | Out-Null

$json = dotnet list $solutionPath package --include-transitive --format json
if ($LASTEXITCODE -ne 0) { throw 'dotnet list package failed.' }
$inventory = $json | ConvertFrom-Json
$packages = @(foreach ($project in $inventory.projects) {
    foreach ($framework in $project.frameworks) {
        foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
            if ($null -ne $package) {
                [pscustomobject]@{ Id = $package.id; Version = $package.resolvedVersion }
            }
        }
    }
}) | Sort-Object Id, Version -Unique

$rows = foreach ($package in $packages) {
    $id = $package.Id.ToLowerInvariant()
    $version = $package.Version.ToLowerInvariant()
    $metadataUri = "https://api.nuget.org/v3/registration5-semver1/$id/$version.json"
    try {
        $registration = Invoke-RestMethod -Uri $metadataUri -TimeoutSec 30
        $metadata = if ($registration.catalogEntry -is [string]) {
            Invoke-RestMethod -Uri $registration.catalogEntry -TimeoutSec 30
        } else { $registration.catalogEntry }
        $license = if ($metadata.licenseExpression) { $metadata.licenseExpression }
            elseif ($metadata.licenseUrl) { $metadata.licenseUrl }
            else { 'REVIEW REQUIRED' }
        [pscustomobject]@{ Package = $package.Id; Version = $package.Version; License = $license; ProjectUrl = $metadata.projectUrl }
    }
    catch {
        [pscustomobject]@{ Package = $package.Id; Version = $package.Version; License = 'LOOKUP FAILED'; ProjectUrl = $metadataUri }
    }
}

$lines = @(
    '# Dependency License Evidence', '',
    "Generated (UTC): $([DateTime]::UtcNow.ToString('O'))", '',
    'This inventory includes direct and transitive NuGet dependencies resolved by the solution. Review any `REVIEW REQUIRED` or `LOOKUP FAILED` entry before release.', '',
    '| Package | Version | License | Project |',
    '|---|---:|---|---|'
)
$lines += $rows | ForEach-Object {
    $projectLink = if ($_.ProjectUrl) { "[link]($($_.ProjectUrl))" } else { '' }
    "| $($_.Package) | $($_.Version) | $($_.License) | $projectLink |"
}
$lines | Set-Content -LiteralPath $outputFullPath -Encoding utf8
Write-Output "Wrote $($rows.Count) dependency records to $outputFullPath"
if ($rows.License -contains 'LOOKUP FAILED' -or $rows.License -contains 'REVIEW REQUIRED') { exit 1 }
