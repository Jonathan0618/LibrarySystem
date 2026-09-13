[CmdletBinding()]
param(
    [string]$ServerInstance = '.\SQLEXPRESS',
    [string]$Database = 'LibrarySystem',
    [int]$MinimumBooks = 1000,
    [int]$MinimumLoans = 10000,
    [int]$MinimumAuditLogs = 10000,
    [int]$MaximumQueryMilliseconds = 2000,
    [string]$EvidencePath = 'artifacts/performance/database-performance.txt'
)

$ErrorActionPreference = 'Stop'
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidencePath))
$evidenceDirectory = Split-Path -Parent $evidenceFullPath
New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null

$queries = @(
    @{ Name = 'Catalog title search'; Sql = "SELECT TOP (50) b.Id, b.Title, b.Isbn FROM dbo.CatalogBooks b WHERE b.IsArchived = 0 AND b.Title LIKE '%library%' ORDER BY b.Title, b.Id;" },
    @{ Name = 'Overdue circulation'; Sql = "SELECT TOP (100) l.Id, l.MemberId, l.BookCopyId, l.DueAtUtc FROM dbo.CirculationLoans l WHERE l.Status IN (1, 3) AND l.DueAtUtc < SYSUTCDATETIME() ORDER BY l.DueAtUtc, l.Id;" },
    @{ Name = 'Recent audit activity'; Sql = "SELECT TOP (100) a.Id, a.Action, a.TargetType, a.CreatedAtUtc FROM dbo.AuditLogs a ORDER BY a.CreatedAtUtc DESC, a.Id DESC;" }
)

function Invoke-Sql([string]$Sql) {
    $output = & $sqlcmd -S $ServerInstance -d $Database -E -b -h -1 -W -Q $Sql 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($output -join [Environment]::NewLine) }
    return ($output -join [Environment]::NewLine).Trim()
}

$countsText = Invoke-Sql "SET NOCOUNT ON; SELECT CONCAT((SELECT COUNT_BIG(*) FROM dbo.CatalogBooks), '|', (SELECT COUNT_BIG(*) FROM dbo.CirculationLoans), '|', (SELECT COUNT_BIG(*) FROM dbo.AuditLogs));"
$countsLine = ($countsText -split "`r?`n" | Where-Object { $_ -match '^\d+\|\d+\|\d+$' } | Select-Object -First 1)
if (-not $countsLine) { throw "Could not parse database row counts: $countsText" }
$counts = $countsLine.Split('|')
$bookCount, $loanCount, $auditCount = [long]$counts[0], [long]$counts[1], [long]$counts[2]

$failures = [System.Collections.Generic.List[string]]::new()
if ($bookCount -lt $MinimumBooks) { $failures.Add("CatalogBooks has $bookCount rows; at least $MinimumBooks are required.") }
if ($loanCount -lt $MinimumLoans) { $failures.Add("CirculationLoans has $loanCount rows; at least $MinimumLoans are required.") }
if ($auditCount -lt $MinimumAuditLogs) { $failures.Add("AuditLogs has $auditCount rows; at least $MinimumAuditLogs are required.") }

$results = foreach ($query in $queries) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $null = Invoke-Sql ("SET NOCOUNT ON; " + $query.Sql)
    $stopwatch.Stop()
    if ($stopwatch.ElapsedMilliseconds -gt $MaximumQueryMilliseconds) {
        $failures.Add("$($query.Name) took $($stopwatch.ElapsedMilliseconds) ms; limit is $MaximumQueryMilliseconds ms.")
    }
    [pscustomobject]@{ Query = $query.Name; ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds }
}

$lines = @(
    "LibrarySystem database performance evidence",
    "GeneratedUtc: $([DateTime]::UtcNow.ToString('O'))",
    "Server: $ServerInstance",
    "Database: $Database",
    "CatalogBooks: $bookCount (minimum $MinimumBooks)",
    "CirculationLoans: $loanCount (minimum $MinimumLoans)",
    "AuditLogs: $auditCount (minimum $MinimumAuditLogs)",
    "MaximumQueryMilliseconds: $MaximumQueryMilliseconds"
)
$lines += $results | ForEach-Object { "$($_.Query): $($_.ElapsedMilliseconds) ms" }
$lines += if ($failures.Count -eq 0) { 'Result: PASS' } else { 'Result: FAIL'; $failures | ForEach-Object { "Failure: $_" } }
$lines | Set-Content -LiteralPath $evidenceFullPath -Encoding utf8
$lines | Write-Output
if ($failures.Count -gt 0) { exit 1 }
