[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BackupDirectory,

    [string]$Server = '.\SQLEXPRESS',

    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$Database = 'LibrarySystem',

    [switch]$UseCompression
)

$ErrorActionPreference = 'Stop'

$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
$directory = New-Item -ItemType Directory -Path $BackupDirectory -Force
$resolvedDirectory = $directory.FullName
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = Join-Path $resolvedDirectory "$Database-$timestamp.bak"
$sqlBackupPath = $backupPath.Replace("'", "''")
$quotedDatabase = $Database.Replace(']', ']]')
$backupOptions = if ($UseCompression) {
    'COPY_ONLY, CHECKSUM, COMPRESSION, INIT, STATS = 10'
} else {
    'COPY_ONLY, CHECKSUM, INIT, STATS = 10'
}

$query = @"
BACKUP DATABASE [$quotedDatabase]
TO DISK = N'$sqlBackupPath'
WITH $backupOptions;
RESTORE VERIFYONLY
FROM DISK = N'$sqlBackupPath'
WITH CHECKSUM;
"@

& $sqlcmd.Source -S $Server -E -b -l 15 -Q $query
if ($LASTEXITCODE -ne 0) {
    throw "Database backup or verification failed with exit code $LASTEXITCODE."
}

$backup = Get-Item -LiteralPath $backupPath
Write-Output "Verified backup: $($backup.FullName) ($($backup.Length) bytes)"
