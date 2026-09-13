[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BackupFile,

    [Parameter(Mandatory)]
    [switch]$ConfirmDrill,

    [string]$Server = '.\SQLEXPRESS',

    [ValidatePattern('^[A-Za-z0-9_-]+_(Performance|Integrity)_RestoreDrill$')]
    [string]$Database = 'LibrarySystem_Performance_RestoreDrill',

    [int]$BookCount = 1000,
    [int]$LoanCount = 10000,
    [int]$AuditLogCount = 10000,
    [int]$MaximumQueryMilliseconds = 2000,
    [switch]$KeepDrillDatabase
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmDrill) { throw 'Supply -ConfirmDrill to authorize the isolated restore and synthetic-data drill.' }
if ($BookCount -lt 1000 -or $LoanCount -lt 10000 -or $AuditLogCount -lt 10000) {
    throw 'Representative defaults require at least 1,000 books, 10,000 loans, and 10,000 audit logs.'
}
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$restoreScript = Join-Path $PSScriptRoot 'Test-DatabaseRestoreDrill.ps1'
$performanceScript = Join-Path $PSScriptRoot 'Test-DatabasePerformance.ps1'
$databaseName = '[' + $Database.Replace(']', ']]') + ']'
$databaseLiteral = $Database.Replace("'", "''")

try {
    & $restoreScript -BackupFile $BackupFile -Server $Server -DrillDatabase $Database -ConfirmDrill -KeepDrillDatabase -EvidencePath 'artifacts/performance/representative-restore.txt'
    if ($LASTEXITCODE -ne 0) { throw 'The representative database restore failed.' }

    $seedSql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

DECLARE @UserId nvarchar(450) = N'performance-test-user';
IF EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Id = @UserId)
    THROW 51000, 'Synthetic performance data already exists.', 1;

INSERT dbo.AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
     SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
     LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, IsActive, CreatedAtUtc, UpdatedAtUtc)
VALUES
    (@UserId, N'performance-test', N'PERFORMANCE-TEST', N'performance@example.invalid',
     N'PERFORMANCE@EXAMPLE.INVALID', 1, NULL, CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
     NULL, 0, 0, NULL, 0, 0, N'Performance', N'Test', 1, SYSUTCDATETIME(), NULL);

INSERT dbo.LibraryMembers (UserId, MemberNumber, MemberType, Grade, Department, IsActive, CreatedAtUtc, UpdatedAtUtc)
VALUES (@UserId, N'PERF-000001', 1, N'Synthetic', NULL, 1, SYSUTCDATETIME(), NULL);
DECLARE @MemberId bigint = SCOPE_IDENTITY();

;WITH numbers AS
(
    SELECT TOP ($BookCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT dbo.CatalogBooks (Isbn, Title, Edition, PublicationYear, Description, CoverImagePath, PublisherId, IsArchived, CreatedAtUtc, UpdatedAtUtc)
SELECT CONCAT(N'PERF-', FORMAT(n, '0000000000')), CONCAT(N'Performance Library Book ', n), NULL,
       2000 + (n % 25), N'Synthetic performance-test record', NULL, NULL, 0, SYSUTCDATETIME(), NULL
FROM numbers;

;WITH books AS
(
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS n
    FROM dbo.CatalogBooks WHERE Isbn LIKE N'PERF-%'
), numbers AS
(
    SELECT TOP ($LoanCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT dbo.CatalogBookCopies (BookId, Barcode, ShelfLocationId, Status, Condition, AcquisitionDate, AcquisitionPrice, CreatedAtUtc, UpdatedAtUtc)
SELECT b.Id, CONCAT(N'PERF-COPY-', FORMAT(n.n, '0000000000')), NULL,
       CASE WHEN n.n % 10 = 0 THEN 2 ELSE 1 END, 2, CAST(SYSUTCDATETIME() AS date), 100.00, SYSUTCDATETIME(), NULL
FROM numbers n
JOIN books b ON b.n = ((n.n - 1) % $BookCount) + 1;

;WITH copies AS
(
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS n
    FROM dbo.CatalogBookCopies WHERE Barcode LIKE N'PERF-COPY-%'
)
INSERT dbo.CirculationLoans
    (CheckoutOperationId, MemberId, BookCopyId, CheckedOutAtUtc, DueAtUtc, ReturnedAtUtc, RenewalCount, Status)
SELECT NEWID(), @MemberId, Id, DATEADD(day, -(30 + (n % 365)), SYSUTCDATETIME()),
       DATEADD(day, -(1 + (n % 60)), SYSUTCDATETIME()),
       CASE WHEN n % 10 = 0 THEN NULL ELSE DATEADD(day, -(n % 30), SYSUTCDATETIME()) END,
       n % 3, CASE WHEN n % 10 = 0 THEN 3 ELSE 2 END
FROM copies;

;WITH numbers AS
(
    SELECT TOP ($AuditLogCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT dbo.AuditLogs (ActorUserId, Action, TargetType, TargetId, Details, CreatedAtUtc)
SELECT @UserId, N'PerformanceTest', N'SyntheticRecord', CONVERT(nvarchar(100), n),
       N'Synthetic performance-test audit entry', DATEADD(second, -n, SYSUTCDATETIME())
FROM numbers;

COMMIT TRANSACTION;
"@
    & $sqlcmd -S $Server -d $Database -E -b -l 30 -Q $seedSql
    if ($LASTEXITCODE -ne 0) { throw 'Representative data generation failed.' }

    & $performanceScript -ServerInstance $Server -Database $Database `
        -MinimumBooks $BookCount -MinimumLoans $LoanCount -MinimumAuditLogs $AuditLogCount `
        -MaximumQueryMilliseconds $MaximumQueryMilliseconds `
        -EvidencePath 'artifacts/performance/representative-database-performance.txt'
    if ($LASTEXITCODE -ne 0) { throw 'Representative database performance gate failed.' }
}
finally {
    if (-not $KeepDrillDatabase) {
        $dropSql = "IF DB_ID(N'$databaseLiteral') IS NOT NULL BEGIN ALTER DATABASE $databaseName SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE $databaseName; END;"
        & $sqlcmd -S $Server -d master -E -b -l 30 -Q $dropSql | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Error "Failed to remove disposable database [$Database]." }
    }
}
