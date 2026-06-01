# Database & EF Core Patterns

## DbContext
- Lives in `Database.MsSql`
- Connection string: `ConnectionStrings__DefaultConnection` via environment variable only
- Never hardcode or put in `appsettings.json`

## Entity Configuration
- Use `IEntityTypeConfiguration<T>` fluent config per entity in `Database.MsSql`
- No data annotations on Domain entities — keep Domain free of EF concerns

## Repository Pattern
Both generic and entity-specific repositories coexist — this is an inherited pattern,
do not consolidate without explicit instruction.

- **Generic:** `IAppCommandRepository` / `IAppReadRepository` — for write and read operations respectively
- **Specific:** `IXxxReadRepository` / `XxxReadRepository` — for complex or domain-specific queries
- Generic interfaces defined in `Application.UseCases/Repositories/Bases/`
- Entity-specific interfaces defined in `Application.UseCases/Repositories/Domain/`
- Implementations in `Database.MsSql`
- Prefer specific repositories for anything beyond basic CRUD

## Query Rules
- `.AsNoTracking()` on all read-only queries — no exceptions
- Always use async methods: `ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`, etc.
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on EF calls
- Raw SQL only via `FromSqlRaw` / `ExecuteSqlRawAsync` with parameterized inputs — never string interpolation

## Migrations
- Code-first — always run `dotnet ef migrations add` to generate
- Always review the generated migration file before applying
- Never modify a migration that has already been applied to a shared or production database
- Add a new corrective migration instead
