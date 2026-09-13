# School Library System

ASP.NET Core .NET 9 school library system using Razor Pages. The implementation roadmap is in `IMPLEMENTATION_PLAN.md`.

## Architecture

The application is a modular monolith with one deployable web project and four supporting class libraries:

```text
LibrarySystem (Razor Pages, Identity UI, startup)
    -> LibrarySystem.Application (interfaces, DTOs, requests)
    -> LibrarySystem.Services (business workflows)
        -> LibrarySystem.Infrastructure (EF Core, Identity, migrations, adapters)
            -> LibrarySystem.Domain (domain enums and concepts)
```

All Razor Pages belong under `LibrarySystem\Pages`. Business service registrations are centralized through `AddLibraryServices()`.

## Implementation status

| Area | Status |
|---|---|
| Project baseline | Complete |
| Identity and database | Partial |
| Member administration | Partial |
| Catalog and inventory | Partial |
| Checkout, return, and renewal | Partial |
| Reservations and member self-service | Partial |
| Fines and notifications | Partial |
| Dashboards, reports, and audit | Partial |
| Production hardening and release | Not started |

See [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md#9-current-implementation-status) for completed work and remaining deliverables for every milestone.

## Prerequisites

- .NET 9 SDK
- SQL Server or SQL Server LocalDB (required beginning with Milestone 1)

## Run locally

```powershell
dotnet restore LibrarySystem.sln
dotnet build LibrarySystem.sln
dotnet run --project LibrarySystem\LibrarySystem.csproj
```

## Development configuration

The checked-in `DefaultConnection` uses the local `SQLEXPRESS` instance with Windows authentication and contains no database password. To use another development server, override it with .NET user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=LibrarySystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project LibrarySystem\LibrarySystem.csproj
```

Do not commit database passwords or production connection strings. Production configuration must provide `ConnectionStrings__DefaultConnection` through the deployment environment's secret store.

Use `LibrarySystem/appsettings.Production.template.json` as the production key/shape reference. Replace its example values through the deployment platform; do not commit a live settings file containing credentials.

Production must also provide a persistent, access-controlled directory for ASP.NET Core Data Protection keys. Startup intentionally fails outside development when this value is missing:

```text
DataProtection__KeysPath=/secure/persistent/library-system-keys
```

The service exposes anonymous probe endpoints for infrastructure monitoring:

- `/health/live` confirms that the process can respond.
- `/health/ready` confirms that the application can reach SQL Server.

Use [alerts.template.json](operations/monitoring/alerts.template.json) as the monitoring-platform mapping and schedule `scripts/Test-MonitoringProbe.ps1` for machine-readable liveness/readiness evidence.

Login requests are limited to five requests per client IP per minute. Production responses include correlation IDs and baseline security headers, and production console logs use structured JSON.

Production notification delivery uses SMTP and validates its configuration during startup. Supply these values through the deployment secret/configuration store:

```text
Notifications__Smtp__Host=smtp.example.edu
Notifications__Smtp__Port=587
Notifications__Smtp__UserName=library@example.edu
Notifications__Smtp__Password=replace-through-secret-store
Notifications__Smtp__FromAddress=library@example.edu
Notifications__Smtp__FromName=School Library
Notifications__Smtp__EnableSsl=true
```

Notification delivery also supports `Notifications__Delivery__MaximumAttempts`, `BatchSize`, `LeaseMinutes`, and `MaximumRetryDelayMinutes`. Defaults are 5 attempts, 50 records, a 5-minute processing lease, and a 60-minute retry-delay cap. See [BACKEND_VERIFICATION.md](BACKEND_VERIFICATION.md) for the integrity guarantees and remaining staging scenarios.

Development continues to capture notification metadata in application logs without logging message bodies.

Outside Development, `AllowedHosts` must list explicit host names rather than `*`. If a reverse proxy terminates TLS or supplies the client address, enable forwarding only for explicitly trusted proxy IPs:

```text
AllowedHosts=library.example.edu
ReverseProxy__Enabled=true
ReverseProxy__KnownProxies__0=10.0.0.10
```

Run the deployed security and health smoke check with:

```powershell
.\scripts\Test-SecurityBaseline.ps1 -BaseUri https://library.example.edu -TestLoginRateLimit
```

Generate dependency-license evidence and run the representative staging database gate with:

```powershell
.\scripts\New-DependencyLicenseEvidence.ps1
.\scripts\Test-DatabasePerformance.ps1 -ServerInstance '.\SQLEXPRESS' -Database 'LibrarySystem'
```

The default performance gate requires 1,000 books, 10,000 loans, and 10,000 audit records and limits each critical query to 2,000 ms. Override these only when approved release targets differ.

For an isolated repeatable baseline, restore a verified backup, generate synthetic representative volume, execute the gate, and remove the disposable database with:

```powershell
.\scripts\Test-RepresentativeDatabasePerformance.ps1 -BackupFile 'D:\SqlBackups\LibrarySystem.bak' -Server 'SQLSERVER-STAGING' -ConfirmDrill
```

The synthetic drill never writes to the source database. Its database name must end in `_Performance_RestoreDrill`, and an existing target is never overwritten.

Automated data retention is disabled by default. Review [DATA_RETENTION_POLICY.md](DATA_RETENTION_POLICY.md), approve the periods, preview eligible records, and take a verified backup before setting `DataRetention__Enabled=true` in production.

Run a recovery exercise into a disposable database whose name ends in `_RestoreDrill`:

```powershell
.\scripts\Test-DatabaseRestoreDrill.ps1 -BackupFile 'D:\SqlBackups\LibrarySystem.bak' -Server 'SQLSERVER-STAGING' -ConfirmDrill
```

The drill verifies the backup checksum, restores to separate files, runs `DBCC CHECKDB`, records the latest EF migration and SHA-256 backup hash, writes evidence under `artifacts/recovery`, and removes the drill database unless `-KeepDrillDatabase` is supplied.

Create and validate a retained application rollback artifact with:

```powershell
.\scripts\New-ReleaseArtifact.ps1 -Version 'approved-release-version'
.\scripts\Test-ApplicationArtifact.ps1 -Artifact 'artifacts/releases/LibrarySystem-approved-release-version.zip' -ConnectionString $stagingConnectionString
```

The artifact manifest records its version, source commit, size, and SHA-256 hash. The validation command verifies that hash, launches the packaged DLL in an isolated temporary directory, and requires liveness and database readiness to pass without recording the connection string.

The optional initial administrator must also be supplied through user secrets. If these values are omitted, role creation still runs but administrator creation is skipped:

```powershell
dotnet user-secrets set "IdentitySeed:AdministratorEmail" "admin@nvsu.edu.ph" --project LibrarySystem\LibrarySystem.csproj
dotnet user-secrets set "IdentitySeed:AdministratorPassword" "replace-with-a-strong-password" --project LibrarySystem\LibrarySystem.csproj
dotnet user-secrets set "IdentitySeed:EnableAdministratorBootstrap" "true" --project LibrarySystem\LibrarySystem.csproj
```

Remove all three bootstrap settings after verification. Production onboarding and rotation procedures are in [ADMINISTRATOR_AND_SECRET_OPERATIONS.md](ADMINISTRATOR_AND_SECRET_OPERATIONS.md).

Temporary librarian seeding is disabled by default and cannot run outside Development. If a disposable local librarian is needed, explicitly configure `IdentitySeed:EnableTemporaryLibrarian=true` together with `IdentitySeed:TemporaryLibrarianEmail` and `IdentitySeed:TemporaryLibrarianPassword` through user secrets.

Identity confirmation and password-reset tokens use the validated `Identity__Links__PublicBaseUrl` and `Identity__Links__TokenLifetimeMinutes` settings. The backend is documented in [IDENTITY_RECOVERY.md](IDENTITY_RECOVERY.md); its public Razor Pages remain deferred to the UI milestone.

See [SECURITY_REVIEW.md](SECURITY_REVIEW.md) for the current route/policy matrix, mutating-handler review, implemented controls, and remaining release checks.

## Database migrations

Restore the repository-local EF tool and apply migrations with:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project LibrarySystem.Infrastructure\LibrarySystem.Infrastructure.csproj --startup-project LibrarySystem\LibrarySystem.csproj
```

Before applying migrations outside local development, back up the database and follow the deployment rollback procedure established during release hardening.

See [OPERATIONS_RUNBOOK.md](OPERATIONS_RUNBOOK.md) for production configuration, reviewed migration artifacts, backup, restore, rollback, health monitoring, smoke checks, and incident records.
