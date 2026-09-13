[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Artifact,

    [Parameter(Mandatory)]
    [string]$ConnectionString,

    [uri]$BaseUri = 'http://127.0.0.1:5290',
    [string]$EvidencePath = 'artifacts/recovery/application-artifact-test.txt'
)

$ErrorActionPreference = 'Stop'
$archive = Get-Item -LiteralPath $Artifact -ErrorAction Stop
if ($archive.Extension -ne '.zip') { throw 'The application artifact must be a .zip file.' }
$manifestPath = "$($archive.FullName).manifest.json"
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Artifact manifest not found: $manifestPath" }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$actualHash = (Get-FileHash -LiteralPath $archive.FullName -Algorithm SHA256).Hash
if ($actualHash -ne $manifest.sha256) { throw 'Artifact SHA-256 does not match its manifest.' }

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidencePath))
New-Item -ItemType Directory -Path (Split-Path -Parent $evidenceFullPath) -Force | Out-Null
$systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$testRoot = Join-Path $systemTemp "LibrarySystem-ArtifactTest-$([Guid]::NewGuid().ToString('N'))"
$extractPath = Join-Path $testRoot 'app'
$keysPath = Join-Path $testRoot 'keys'
$stdoutPath = Join-Path $testRoot 'stdout.log'
$stderrPath = Join-Path $testRoot 'stderr.log'
New-Item -ItemType Directory -Path $extractPath, $keysPath -Force | Out-Null
$process = $null
$startedUtc = [DateTime]::UtcNow
$result = 'FAIL'

try {
    Expand-Archive -LiteralPath $archive.FullName -DestinationPath $extractPath
    $applicationDll = Join-Path $extractPath 'LibrarySystem.dll'
    if (-not (Test-Path -LiteralPath $applicationDll)) { throw 'LibrarySystem.dll is missing from the artifact.' }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new('dotnet', "`"$applicationDll`"")
    $startInfo.WorkingDirectory = $extractPath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Staging'
    $startInfo.Environment['ASPNETCORE_URLS'] = $BaseUri.AbsoluteUri.TrimEnd('/')
    $startInfo.Environment['AllowedHosts'] = $BaseUri.Host
    $startInfo.Environment['ConnectionStrings__DefaultConnection'] = $ConnectionString
    $startInfo.Environment['DataProtection__KeysPath'] = $keysPath
    $startInfo.Environment['ReverseProxy__Enabled'] = 'false'
    $startInfo.Environment['Logging__LogLevel__Default'] = 'Warning'
    $startInfo.Environment['Notifications__Smtp__Host'] = '127.0.0.1'
    $startInfo.Environment['Notifications__Smtp__Port'] = '25'
    $startInfo.Environment['Notifications__Smtp__UserName'] = 'artifact-test'
    $startInfo.Environment['Notifications__Smtp__Password'] = 'artifact-test-only'
    $startInfo.Environment['Notifications__Smtp__FromAddress'] = 'artifact-test@example.invalid'
    $startInfo.Environment['Notifications__Smtp__FromName'] = 'Artifact Test'
    $startInfo.Environment['Notifications__Smtp__EnableSsl'] = 'false'
    $process = [System.Diagnostics.Process]::Start($startInfo)

    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    $ready = $false
    while ([DateTime]::UtcNow -lt $deadline -and -not $process.HasExited) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri ([uri]::new($BaseUri, '/health/ready')) -TimeoutSec 3
            if ($response.StatusCode -eq 200 -and $response.Content.Trim() -eq 'Healthy') { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'Artifact did not become database-ready within 45 seconds.' }
    $live = Invoke-WebRequest -UseBasicParsing -Uri ([uri]::new($BaseUri, '/health/live')) -TimeoutSec 5
    if ($live.StatusCode -ne 200 -or $live.Content.Trim() -ne 'Healthy') { throw 'Artifact liveness check failed.' }
    $result = 'PASS'
}
finally {
    if ($process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit(10000) | Out-Null }
    if ($process) {
        $process.StandardOutput.ReadToEnd() | Set-Content -LiteralPath $stdoutPath -Encoding utf8
        $process.StandardError.ReadToEnd() | Set-Content -LiteralPath $stderrPath -Encoding utf8
    }
    $finishedUtc = [DateTime]::UtcNow
    @(
        'LibrarySystem application artifact test evidence',
        "StartedUtc: $($startedUtc.ToString('O'))",
        "FinishedUtc: $($finishedUtc.ToString('O'))",
        "Artifact: $($archive.FullName)",
        "Version: $($manifest.version)",
        "GitCommit: $($manifest.gitCommit)",
        "Sha256: $actualHash",
        "BaseUri: $BaseUri",
        'DatabaseConnection: supplied (value intentionally omitted)',
        "Result: $result"
    ) | Set-Content -LiteralPath $evidenceFullPath -Encoding utf8

    $resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
    if ($resolvedTestRoot.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTestRoot) -like 'LibrarySystem-ArtifactTest-*') {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    } else { throw "Refusing to remove unexpected temporary path: $resolvedTestRoot" }
}

Get-Content -LiteralPath $evidenceFullPath
