# School Library System

ASP.NET Core .NET 9 school library system using Razor Pages. The implementation roadmap is in `IMPLEMENTATION_PLAN.md`.

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

The optional initial administrator must also be supplied through user secrets. If these values are omitted, role creation still runs but administrator creation is skipped:

```powershell
dotnet user-secrets set "IdentitySeed:AdministratorEmail" "admin@example.edu" --project LibrarySystem\LibrarySystem.csproj
dotnet user-secrets set "IdentitySeed:AdministratorPassword" "replace-with-a-strong-password" --project LibrarySystem\LibrarySystem.csproj
```

## Database migrations

Restore the repository-local EF tool and apply migrations with:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project LibrarySystem.Infrastructure\LibrarySystem.Infrastructure.csproj --startup-project LibrarySystem\LibrarySystem.csproj
```

Before applying migrations outside local development, back up the database and follow the deployment rollback procedure established during release hardening.
