[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BackupFile,

    [Parameter(Mandatory)]
    [switch]$ConfirmDrill,

    [string]$Server = '.\SQLEXPRESS',

    [ValidatePattern('^[A-Za-z0-9_-]+_RestoreDrill$')]
    [string]$DrillDatabase = 'LibrarySystem_RestoreDrill',

    [switch]$KeepDrillDatabase,

    [string]$EvidencePath = 'artifacts/recovery/restore-drill.txt'
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmDrill) { throw 'Supply -ConfirmDrill to authorize creation of the disposable drill database.' }
if ($DrillDatabase -notmatch '_RestoreDrill$') { throw 'The drill database name must end with _RestoreDrill.' }

$backup = Get-Item -LiteralPath $BackupFile -ErrorAction Stop
if ($backup.Extension -ne '.bak') { throw 'The backup file must use the .bak extension.' }
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidencePath))
New-Item -ItemType Directory -Path (Split-Path -Parent $evidenceFullPath) -Force | Out-Null

function Escape-SqlLiteral([string]$Value) { return $Value.Replace("'", "''") }
function Quote-SqlName([string]$Value) { return '[' + $Value.Replace(']', ']]') + ']' }
function Invoke-Sql([string]$Query, [string]$Database = 'master') {
    $output = & $sqlcmd -S $Server -d $Database -E -b -l 30 -h -1 -W -s '|' -Q $Query 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($output -join [Environment]::NewLine) }
    return @($output)
}

$backupSql = Escape-SqlLiteral $backup.FullName
$databaseSql = Escape-SqlLiteral $DrillDatabase
$databaseName = Quote-SqlName $DrillDatabase
$existing = (Invoke-Sql "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'$databaseSql';" | Select-Object -First 1).Trim()
if ($existing -ne '0') { throw "Drill database [$DrillDatabase] already exists. Remove it explicitly after confirming its ownership; this script will not overwrite it." }

$fileRows = Invoke-Sql "RESTORE FILELISTONLY FROM DISK = N'$backupSql';"
$files = foreach ($row in $fileRows) {
    $parts = $row -split '\|'
    if ($parts.Count -ge 3 -and $parts[2].Trim() -in @('D', 'L')) {
        [pscustomobject]@{ LogicalName = $parts[0].Trim(); Type = $parts[2].Trim() }
    }
}
$dataFile = $files | Where-Object Type -eq 'D' | Select-Object -First 1
$logFile = $files | Where-Object Type -eq 'L' | Select-Object -First 1
if (-not $dataFile -or -not $logFile) { throw 'Could not identify the logical data and log files in the backup.' }

$paths = (Invoke-Sql "SET NOCOUNT ON; SELECT CONCAT(CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000)), '|', CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(4000)));" | Where-Object { $_ -match '\|' } | Select-Object -First 1).Split('|')
$dataPath = Join-Path $paths[0].Trim() "$DrillDatabase.mdf"
$logPath = Join-Path $paths[1].Trim() "${DrillDatabase}_log.ldf"
$dataLogical = Escape-SqlLiteral $dataFile.LogicalName
$logLogical = Escape-SqlLiteral $logFile.LogicalName
$dataPathSql = Escape-SqlLiteral $dataPath
$logPathSql = Escape-SqlLiteral $logPath
$startedUtc = [DateTime]::UtcNow
$result = 'FAIL'
$details = [System.Collections.Generic.List[string]]::new()

try {
    $restoreQuery = @"
RESTORE VERIFYONLY FROM DISK = N'$backupSql' WITH CHECKSUM;
RESTORE DATABASE $databaseName FROM DISK = N'$backupSql'
WITH MOVE N'$dataLogical' TO N'$dataPathSql',
     MOVE N'$logLogical' TO N'$logPathSql',
     RECOVERY, CHECKSUM, STATS = 10;
DBCC CHECKDB ($databaseName) WITH PHYSICAL_ONLY, NO_INFOMSGS;
"@
    $details.AddRange([string[]](Invoke-Sql $restoreQuery))
    $migration = (Invoke-Sql 'SET NOCOUNT ON; SELECT TOP (1) MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId DESC;' $DrillDatabase | Where-Object { $_ -match '^\d{14}_' } | Select-Object -First 1).Trim()
    if (-not $migration) { throw 'The restored database has no readable EF migration history.' }
    $result = 'PASS'
}
finally {
    $finishedUtc = [DateTime]::UtcNow
    $hash = (Get-FileHash -LiteralPath $backup.FullName -Algorithm SHA256).Hash
    $lines = @(
        'LibrarySystem database restore drill evidence',
        "StartedUtc: $($startedUtc.ToString('O'))",
        "FinishedUtc: $($finishedUtc.ToString('O'))",
        "Server: $Server",
        "DrillDatabase: $DrillDatabase",
        "BackupFile: $($backup.FullName)",
        "BackupBytes: $($backup.Length)",
        "BackupSha256: $hash",
        "LatestMigration: $migration",
        "Result: $result"
    )
    $lines += $details
    $lines | Set-Content -LiteralPath $evidenceFullPath -Encoding utf8

    if (-not $KeepDrillDatabase) {
        $dropQuery = "IF DB_ID(N'$databaseSql') IS NOT NULL BEGIN ALTER DATABASE $databaseName SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE $databaseName; END;"
        Invoke-Sql $dropQuery | Out-Null
    }
}

Get-Content -LiteralPath $evidenceFullPath
