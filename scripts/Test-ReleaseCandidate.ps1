[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$BaseUri,

    [string]$Artifact,
    [string]$ConnectionString,
    [string]$PerformanceBackupFile,
    [string]$SqlServer = '.\SQLEXPRESS',
    [switch]$TestLoginRateLimit,
    [string]$EvidencePath = 'artifacts/release/release-candidate.json'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidencePath))
New-Item -ItemType Directory -Path (Split-Path -Parent $evidenceFullPath) -Force | Out-Null
$results = [System.Collections.Generic.List[object]]::new()
$startedUtc = [DateTime]::UtcNow

function Invoke-Gate([string]$Name, [scriptblock]$Action) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action | Out-Host
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "$Name exited with code $LASTEXITCODE." }
        $stopwatch.Stop()
        $results.Add([ordered]@{ name = $Name; passed = $true; elapsedMilliseconds = $stopwatch.ElapsedMilliseconds })
    }
    catch {
        $stopwatch.Stop()
        $results.Add([ordered]@{
            name = $Name
            passed = $false
            elapsedMilliseconds = $stopwatch.ElapsedMilliseconds
            error = $_.Exception.Message
        })
    }
}

Push-Location $repositoryRoot
try {
    Invoke-Gate 'release-build' {
        dotnet build LibrarySystem.sln --configuration Release --no-restore -v:minimal
    }
    Invoke-Gate 'migration-model' {
        dotnet ef migrations has-pending-model-changes --project LibrarySystem.Infrastructure --startup-project LibrarySystem --no-build --configuration Release
    }
    Invoke-Gate 'dependency-vulnerabilities' {
        $output = dotnet list LibrarySystem.sln package --vulnerable --include-transitive
        $output | Out-Host
        if ($output -match 'has the following vulnerable packages') { throw 'Vulnerable NuGet packages were reported.' }
    }
    Invoke-Gate 'dependency-licenses' {
        & (Join-Path $PSScriptRoot 'New-DependencyLicenseEvidence.ps1')
    }
    Invoke-Gate 'security-baseline' {
        $arguments = @{ BaseUri = $BaseUri }
        if ($TestLoginRateLimit) { $arguments.TestLoginRateLimit = $true }
        & (Join-Path $PSScriptRoot 'Test-SecurityBaseline.ps1') @arguments
    }
    Invoke-Gate 'monitoring-probe' {
        & (Join-Path $PSScriptRoot 'Test-MonitoringProbe.ps1') -BaseUri $BaseUri
    }

    if ($Artifact -or $ConnectionString) {
        if (-not $Artifact -or -not $ConnectionString) {
            $results.Add([ordered]@{ name = 'rollback-artifact'; passed = $false; error = 'Artifact and ConnectionString must be supplied together.' })
        }
        else {
            Invoke-Gate 'rollback-artifact' {
                & (Join-Path $PSScriptRoot 'Test-ApplicationArtifact.ps1') -Artifact $Artifact -ConnectionString $ConnectionString
            }
        }
    }

    if ($PerformanceBackupFile) {
        Invoke-Gate 'representative-performance' {
            & (Join-Path $PSScriptRoot 'Test-RepresentativeDatabasePerformance.ps1') `
                -BackupFile $PerformanceBackupFile -Server $SqlServer -ConfirmDrill
        }
    }
}
finally {
    Pop-Location
}

$passed = -not ($results | Where-Object { -not $_.passed })
[ordered]@{
    application = 'LibrarySystem'
    startedUtc = $startedUtc.ToString('O')
    finishedUtc = [DateTime]::UtcNow.ToString('O')
    baseUri = $BaseUri.AbsoluteUri
    passed = $passed
    gates = $results
    manualEvidenceRequired = @(
        'role-by-role authorization',
        'controlled checkout and return',
        'concurrency and stale-row-version scenarios',
        'SMTP delivery and recovery',
        'retained-version staging rollback',
        'librarian and member acceptance sign-off'
    )
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $evidenceFullPath -Encoding utf8

Get-Content -LiteralPath $evidenceFullPath
if (-not $passed) { exit 1 }
