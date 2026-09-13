[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$BaseUri,

    [switch]$TestLoginRateLimit
)

$ErrorActionPreference = 'Stop'
$base = $BaseUri.AbsoluteUri.TrimEnd('/')

function Invoke-Check {
    param([string]$Path)

    return Invoke-WebRequest -Uri "$base$Path" -UseBasicParsing -TimeoutSec 15
}

function Assert-Equal {
    param([string]$Name, $Actual, $Expected)

    if ($Actual -ne $Expected) {
        throw "$Name failed. Expected '$Expected', received '$Actual'."
    }
}

$live = Invoke-Check '/health/live'
$ready = Invoke-Check '/health/ready'
Assert-Equal 'Liveness status' $live.StatusCode 200
Assert-Equal 'Liveness body' $live.Content 'Healthy'
Assert-Equal 'Readiness status' $ready.StatusCode 200
Assert-Equal 'Readiness body' $ready.Content 'Healthy'
Assert-Equal 'MIME-sniffing header' $live.Headers['X-Content-Type-Options'] 'nosniff'
Assert-Equal 'Frame protection header' $live.Headers['X-Frame-Options'] 'DENY'
Assert-Equal 'Cross-origin opener header' $live.Headers['Cross-Origin-Opener-Policy'] 'same-origin'
Assert-Equal 'Cross-origin resource header' $live.Headers['Cross-Origin-Resource-Policy'] 'same-origin'

$csp = $live.Headers['Content-Security-Policy']
if ([string]::IsNullOrWhiteSpace($csp)) {
    throw 'Content-Security-Policy header is missing.'
}
if ($csp -match "'unsafe-inline'" -or $csp -notmatch "script-src 'self'") {
    throw 'Content-Security-Policy permits inline code or does not restrict scripts to this origin.'
}

if ($BaseUri.Scheme -eq 'https' -and $BaseUri.Host -ne 'localhost' -and
    [string]::IsNullOrWhiteSpace($live.Headers['Strict-Transport-Security'])) {
    throw 'HSTS is missing from the HTTPS staging response.'
}

$login = Invoke-Check '/Identity/Account/Login'
$setCookie = [string]$login.Headers['Set-Cookie']
if ($setCookie -notmatch '(?i)antiforgery' -or
    $setCookie -notmatch '(?i)secure' -or
    $setCookie -notmatch '(?i)httponly' -or
    $setCookie -notmatch '(?i)samesite=strict') {
    throw 'The antiforgery cookie is missing Secure, HttpOnly, or SameSite=Strict.'
}
if ($login.Content -notmatch '__RequestVerificationToken') {
    throw 'The login form does not contain an antiforgery token.'
}

$staffPaths = @(
    '/Dashboards/Members',
    '/Dashboards/Books',
    '/Dashboards/Checkouts',
    '/Dashboards/Reservations',
    '/Dashboards/Fines',
    '/Dashboards/Reports',
    '/Dashboards/AuditLog',
    '/Dashboards/Roles',
    '/Dashboards/Inventory',
    '/Dashboards/Acquisitions'
)
foreach ($path in $staffPaths) {
    $response = Invoke-WebRequest -Uri "$base$path" -UseBasicParsing -TimeoutSec 15
    $finalPath = $response.BaseResponse.ResponseUri.AbsolutePath
    if ($finalPath -ne '/Identity/Account/Login') {
        throw "Anonymous request to $path was not denied; it ended at $finalPath with status $($response.StatusCode)."
    }
}

$correlationId = [guid]::Empty
if (-not [guid]::TryParse($live.Headers['X-Correlation-ID'], [ref]$correlationId)) {
    throw 'X-Correlation-ID is missing or is not a GUID.'
}

try {
    Invoke-WebRequest -Uri "$base/Identity/Account/Login" -Method Post `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body 'Input.Email=security-check%40invalid.example&Input.Password=invalid' `
        -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop | Out-Null
    throw 'A tokenless login POST was accepted.'
}
catch {
    if ($_.Exception.Response.StatusCode -ne 400) {
        throw
    }
}

if ($TestLoginRateLimit) {
    $statuses = 1..6 | ForEach-Object {
        try {
            (Invoke-Check '/Identity/Account/Login').StatusCode
        }
        catch {
            [int]$_.Exception.Response.StatusCode
        }
    }
    Assert-Equal 'Login rate limit' $statuses[-1] 429
}

Write-Output "Security baseline passed for $base."
