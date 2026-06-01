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
- Building & running the project → @.claude/building_the_project.md
- Code conventions → @.claude/code_conventions.md
- Service architecture & request pipeline → @.claude/service_architecture.md
- Database & EF Core patterns → @.claude/database_patterns.md
- Blazor conventions & project structure → @.claude/blazor_conventions.md
- Auth (Cookie) → @.claude/auth.md
- SAP integration → @.claude/sap_integration.md
- Known architectural debts → @.claude/architectural_debts.md
- Domain layer internals → @.claude/domain_layer.md
- Application layer internals → @.claude/application_layer.md
- UI abstractions & workflow → @.claude/ui_abstractions.md

## Adding New Features or Fixing Bugs
> **IMPORTANT**: When we work on a new feature or bug, create a git branch first. Then work changes in that branch for the remainder of the session. The branch name should be `claude/feat/feature-name` for a new feature or `claude/fix/issue-log-number` for a bug or issue log.

## Build & Commit Workflow
After completing any implementation, always follow this sequence:

1. **Verify the shell is functional first** — run a simple probe command (e.g. `echo ok`) before attempting `dotnet build`.
   - If the probe returns no output and exits with code 1, the shell environment is broken. **Stop immediately** — do not loop, do not retry build commands. Report the shell failure to the user and ask them to run `dotnet build` manually and paste the output.
   - If the probe succeeds, proceed to step 2.

2. **Build** — run `dotnet build` from the solution root and capture full output.
   - If the build output is empty and the exit code is 1 with no compiler errors shown, treat this as a shell failure (same as above) — stop and report.

3. **If the build fails with real compiler errors** — read the errors, fix them, and rebuild. Cap fix iterations at **3 attempts**. If still failing after 3 attempts, stop and report the remaining errors to the user instead of continuing to loop.

4. **If the build succeeds** — automatically (no explicit ask needed):
   - Create or switch to the `claude/feat/` or `claude/fix/` branch
   - Stage the specific changed files
   - Commit with a short imperative message + co-author trailer
   - Push to `origin` with `-u` if the branch is new, or a plain `git push` if it already tracks a remote

   Never commit or push to `master` or the user's active feature branch. The user will merge the claude branch into their own branch via lazygit or their preferred tool.

> All commits land on a `claude/` branch — never on the user's working branch.
> Push to origin is automatic after every successful build commit.

## Commit Rules
- Commit automatically after every successful build — no explicit ask needed
- Use short imperative summary messages (e.g. `feat: add goods receipt query`, `fix: correct role permission cascade`)
- Always include co-author: `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>`
- Stage specific files — never `git add .` or `git add -A`

