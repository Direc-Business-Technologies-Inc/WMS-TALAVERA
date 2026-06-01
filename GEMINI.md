# LSMS-v4

## Stack
- .NET 8 · Blazor Server · EF Core (MSSQL) · MediatR · Mapster · Ardalis.Guards · Cookie Authentication · Radzen · B1SLayer (SAP) · Dapper

## Solution Map
```
Domain.Entities/                 # DDD aggregates, entities, value objects, domain events, enums
Application.DataTransferObjects/ # DTOs only
Application.UseCases/            # MediatR handlers, IXxxService (internal), Mapster profiles
Shared.Libraries/                # Utilities and extension methods
Shared.DataCipher/               # Data encryption utilities
Database.Libraries/              # EF Core abstractions, ISqlQueryManager
Database.MsSql/                  # DbContext, migrations, repositories
Integration.SAP/                 # SAP HTTP client (outbound only); SQLScripts/ for shared SQL scripts
Web.BlazorServer/                # Blazor Server, Cookie Authentication, Handlers/
```

## Agent Docs
- Building & running the project → @.gemini/building_the_project.md
- Code conventions → @.gemini/code_conventions.md
- Service architecture & request pipeline → @.gemini/service_architecture.md
- Database & EF Core patterns → @.gemini/database_patterns.md
- Blazor conventions & project structure → @.gemini/blazor_conventions.md
- Auth (Cookie) → @.gemini/auth.md
- SAP integration → @.gemini/sap_integration.md
- Known architectural debts → @.gemini/architectural_debts.md
- Domain layer internals → @.gemini/domain_layer.md
- Application layer internals → @.gemini/application_layer.md
- UI abstractions & workflow → @.gemini/ui_abstractions.md

## Adding New Features or Fixing Bugs
> **IMPORTANT**: When we work on a new feature or bug, create a git branch first. Then work changes in that branch for the remainder of the session. The branch name should be `gemini/feat/feature-name` for a new feature or `gemini/fix/issue-log-number` for a bug or issue log.

## Commit Rules
- Use short imperative summary messages (e.g. `feat: add goods receipt query`, `fix: correct role permission cascade`)
- Always include co-author: `Co-Authored-By: Gemini 1.5 Pro <noreply@google.com>`
- Stage specific files — never `git add .` or `git add -A`
