[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$BaseUri,

    [int]$MaximumReadyMilliseconds = 2000,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$startedUtc = [DateTime]::UtcNow
$checks = [System.Collections.Generic.List[object]]::new()
$healthy = $true

foreach ($probe in @(
    @{ Name = 'liveness'; Path = '/health/live'; Maximum = 1000 },
    @{ Name = 'readiness'; Path = '/health/ready'; Maximum = $MaximumReadyMilliseconds }
)) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri ([uri]::new($BaseUri, $probe.Path)) -TimeoutSec 10
        $stopwatch.Stop()
        $passed = $response.StatusCode -eq 200 -and
            $response.Content.Trim() -eq 'Healthy' -and
            $stopwatch.ElapsedMilliseconds -le $probe.Maximum
        $checks.Add([ordered]@{
            name = $probe.Name
            statusCode = $response.StatusCode
            elapsedMilliseconds = $stopwatch.ElapsedMilliseconds
            maximumMilliseconds = $probe.Maximum
            passed = $passed
        })
        if (-not $passed) { $healthy = $false }
    }
    catch {
        $stopwatch.Stop()
        $healthy = $false
        $checks.Add([ordered]@{
            name = $probe.Name
            statusCode = $null
            elapsedMilliseconds = $stopwatch.ElapsedMilliseconds
            maximumMilliseconds = $probe.Maximum
            passed = $false
            errorType = $_.Exception.GetType().Name
        })
    }
}

$evidence = [ordered]@{
    application = 'LibrarySystem'
    timestampUtc = $startedUtc.ToString('O')
    baseUri = $BaseUri.AbsoluteUri
    healthy = $healthy
    checks = $checks
}
$json = $evidence | ConvertTo-Json -Depth 5 -Compress
if ($OutputPath) {
    $repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $fullOutputPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
    New-Item -ItemType Directory -Path (Split-Path -Parent $fullOutputPath) -Force | Out-Null
    $json | Set-Content -LiteralPath $fullOutputPath -Encoding utf8
}
$json
if (-not $healthy) { exit 1 }
