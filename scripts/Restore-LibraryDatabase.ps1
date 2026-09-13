[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BackupFile,

    [Parameter(Mandatory)]
    [switch]$ConfirmRestore,

    [string]$Server = '.\SQLEXPRESS',

    [ValidatePattern('^[A-Za-z0-9_-]+$')]
    [string]$Database = 'LibrarySystem'
)

$ErrorActionPreference = 'Stop'

if (-not $ConfirmRestore) {
    throw 'Restore was not confirmed. Supply -ConfirmRestore to acknowledge that the target database will be overwritten.'
}

$backup = Get-Item -LiteralPath $BackupFile -ErrorAction Stop
if ($backup.Extension -ne '.bak') {
    throw 'The backup file must use the .bak extension.'
}

$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
$sqlBackupPath = $backup.FullName.Replace("'", "''")
$quotedDatabase = $Database.Replace(']', ']]')

$query = @"
RESTORE VERIFYONLY
FROM DISK = N'$sqlBackupPath'
WITH CHECKSUM;

ALTER DATABASE [$quotedDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
BEGIN TRY
    RESTORE DATABASE [$quotedDatabase]
    FROM DISK = N'$sqlBackupPath'
    WITH REPLACE, RECOVERY, CHECKSUM, STATS = 10;
    ALTER DATABASE [$quotedDatabase] SET MULTI_USER;
END TRY
BEGIN CATCH
    IF DB_ID(N'$($Database.Replace("'", "''"))') IS NOT NULL
        ALTER DATABASE [$quotedDatabase] SET MULTI_USER;
    THROW;
END CATCH;
"@

& $sqlcmd.Source -S $Server -E -b -l 15 -Q $query
if ($LASTEXITCODE -ne 0) {
    throw "Database restore failed with exit code $LASTEXITCODE."
}

Write-Output "Restored database [$Database] from $($backup.FullName)."
