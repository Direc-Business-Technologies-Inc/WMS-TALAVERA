# Building & Running the Project

## Build
```bash
dotnet build
```

## Run
```bash
dotnet run --project Web.BlazorServer
```

## EF Core Migrations
Always run from the solution root.

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project MsSql \
  --startup-project Web.BlazorServer

# Apply migrations
dotnet ef database update \
  --project MsSql \
  --startup-project Web.BlazorServer
```

## Environment Variables Required
The following must be set before running — never put these in `appsettings.json`:
- `ConnectionStrings__DefaultConnection` — MSSQL connection string

## appsettings.json
Committed to git. Contains only non-sensitive config (logging, feature flags, etc.).
Sensitive values are injected via environment variables at runtime.
