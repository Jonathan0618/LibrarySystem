[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BackupFile,

    [Parameter(Mandatory)]
    [switch]$ConfirmDrill,

    [string]$Server = '.\SQLEXPRESS',

    [ValidatePattern('^[A-Za-z0-9_-]+_Integrity_RestoreDrill$')]
    [string]$Database = 'LibrarySystem_Integrity_RestoreDrill',

    [string]$EvidencePath = 'artifacts/backend/backend-integrity-drill.txt'
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmDrill) { throw 'Supply -ConfirmDrill to authorize the isolated integrity drill.' }
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$representativeScript = Join-Path $PSScriptRoot 'Test-RepresentativeDatabasePerformance.ps1'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceFullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidencePath))
New-Item -ItemType Directory -Path (Split-Path -Parent $evidenceFullPath) -Force | Out-Null
$databaseName = '[' + $Database.Replace(']', ']]') + ']'
$databaseLiteral = $Database.Replace("'", "''")
$startedUtc = [DateTime]::UtcNow
$result = 'FAIL'
$checkOutput = @()

try {
    & $representativeScript -BackupFile $BackupFile -Server $Server -Database $Database `
        -ConfirmDrill -KeepDrillDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Unable to prepare the isolated integrity database.' }

    $integritySql = @"
SET NOCOUNT ON;
SET XACT_ABORT OFF;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @MemberId bigint = (SELECT TOP (1) Id FROM dbo.LibraryMembers WHERE MemberNumber = N'PERF-000001');
DECLARE @CopyId bigint = (SELECT TOP (1) Id FROM dbo.CatalogBookCopies WHERE Barcode LIKE N'PERF-COPY-%' ORDER BY Id);
DECLARE @LoanId bigint = (SELECT TOP (1) Id FROM dbo.CirculationLoans WHERE MemberId = @MemberId ORDER BY Id);
DECLARE @BookId bigint = (SELECT TOP (1) Id FROM dbo.CatalogBooks WHERE Isbn LIKE N'PERF-%' ORDER BY Id);
IF @MemberId IS NULL OR @CopyId IS NULL OR @LoanId IS NULL OR @BookId IS NULL
    THROW 51000, 'Synthetic integrity fixtures are missing.', 1;

-- A failed multi-step transaction leaves no partial audit record.
BEGIN TRANSACTION;
INSERT dbo.AuditLogs (ActorUserId, Action, TargetType, TargetId, Details, CreatedAtUtc)
VALUES (NULL, N'RollbackProbe', N'IntegrityDrill', N'rollback-probe', NULL, SYSUTCDATETIME());
ROLLBACK TRANSACTION;
IF EXISTS (SELECT 1 FROM dbo.AuditLogs WHERE TargetId = N'rollback-probe')
    THROW 51001, 'Transaction rollback left a partial record.', 1;

-- A stale row version cannot update the same book twice.
DECLARE @RowVersion binary(8) = (SELECT RowVersion FROM dbo.CatalogBooks WHERE Id = @BookId);
BEGIN TRANSACTION;
UPDATE dbo.CatalogBooks SET Title = Title + N' A' WHERE Id = @BookId AND RowVersion = @RowVersion;
IF @@ROWCOUNT <> 1 THROW 51002, 'Initial row-version update failed.', 1;
UPDATE dbo.CatalogBooks SET Title = Title + N' B' WHERE Id = @BookId AND RowVersion = @RowVersion;
IF @@ROWCOUNT <> 0 THROW 51003, 'Stale row-version update was accepted.', 1;
ROLLBACK TRANSACTION;

-- Checkout replay uniqueness is enforced by operation and copy.
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @OperationId uniqueidentifier = NEWID();
    INSERT dbo.CirculationLoans (CheckoutOperationId, MemberId, BookCopyId, CheckedOutAtUtc, DueAtUtc, ReturnedAtUtc, RenewalCount, Status)
    VALUES (@OperationId, @MemberId, @CopyId, SYSUTCDATETIME(), DATEADD(day, 14, SYSUTCDATETIME()), NULL, 0, 1);
    INSERT dbo.CirculationLoans (CheckoutOperationId, MemberId, BookCopyId, CheckedOutAtUtc, DueAtUtc, ReturnedAtUtc, RenewalCount, Status)
    VALUES (@OperationId, @MemberId, @CopyId, SYSUTCDATETIME(), DATEADD(day, 14, SYSUTCDATETIME()), NULL, 0, 1);
    ROLLBACK TRANSACTION;
    THROW 51004, 'Duplicate checkout operation/copy was accepted.', 1;
END TRY
BEGIN CATCH
    DECLARE @CheckoutError int = ERROR_NUMBER();
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF @CheckoutError NOT IN (2601, 2627) THROW;
END CATCH;

-- A loan cannot receive the same fine type twice.
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT dbo.Fines (MemberId, LoanId, Type, AssessedAmount, Balance, Status, Reason, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@MemberId, @LoanId, 3, 10.00, 10.00, 1, N'Integrity drill', SYSUTCDATETIME(), NULL);
    INSERT dbo.Fines (MemberId, LoanId, Type, AssessedAmount, Balance, Status, Reason, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@MemberId, @LoanId, 3, 10.00, 10.00, 1, N'Integrity drill duplicate', SYSUTCDATETIME(), NULL);
    ROLLBACK TRANSACTION;
    THROW 51005, 'Duplicate loan/type fine was accepted.', 1;
END TRY
BEGIN CATCH
    DECLARE @FineError int = ERROR_NUMBER();
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF @FineError NOT IN (2601, 2627) THROW;
END CATCH;

-- Notification deduplication survives concurrent/application mistakes.
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT dbo.QueuedNotifications (DeduplicationKey, Type, RecipientEmail, Subject, Body, Status, AttemptCount, CreatedAtUtc, NextAttemptAtUtc, SentAtUtc, LastError)
    VALUES (N'integrity-drill-key', 1, N'test@example.invalid', N'Test', N'Test', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), NULL, NULL);
    INSERT dbo.QueuedNotifications (DeduplicationKey, Type, RecipientEmail, Subject, Body, Status, AttemptCount, CreatedAtUtc, NextAttemptAtUtc, SentAtUtc, LastError)
    VALUES (N'integrity-drill-key', 1, N'test@example.invalid', N'Test', N'Test', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), NULL, NULL);
    ROLLBACK TRANSACTION;
    THROW 51006, 'Duplicate notification key was accepted.', 1;
END TRY
BEGIN CATCH
    DECLARE @NotificationError int = ERROR_NUMBER();
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF @NotificationError NOT IN (2601, 2627) THROW;
END CATCH;

SELECT N'PASS|transaction-rollback|row-version|checkout-idempotency|fine-deduplication|notification-deduplication';
"@
    $checkOutput = & $sqlcmd -S $Server -d $Database -E -b -l 30 -h -1 -W -Q $integritySql 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($checkOutput -join [Environment]::NewLine) }
    if (-not ($checkOutput -match '^PASS\|')) { throw 'The integrity drill did not return its success marker.' }
    $result = 'PASS'
}
finally {
    $finishedUtc = [DateTime]::UtcNow
    @(
        'LibrarySystem backend integrity drill evidence',
        "StartedUtc: $($startedUtc.ToString('O'))",
        "FinishedUtc: $($finishedUtc.ToString('O'))",
        "Server: $Server",
        "Database: $Database",
        "Result: $result"
    ) + $checkOutput | Set-Content -LiteralPath $evidenceFullPath -Encoding utf8

    $dropSql = "IF DB_ID(N'$databaseLiteral') IS NOT NULL BEGIN ALTER DATABASE $databaseName SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE $databaseName; END;"
    & $sqlcmd -S $Server -d master -E -b -l 30 -Q $dropSql | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to remove disposable database [$Database]." }
}

Get-Content -LiteralPath $evidenceFullPath
