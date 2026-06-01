# Service Architecture

## Full Request Pipeline
```
Blazor Component (inherits BaseComponent)
  → @inject IXxxHandler (Handlers/Repositories/ — per-feature interface)
    → XxxHandler (Handlers/Implementations/ — calls IMediator.Send())
      → Application.UseCases Handler
        → IXxxService (internal Application-layer contracts)
          → IAppCommandRepository / IAppReadRepository (Application.UseCases/Repositories/Bases/)
            → AppCommandRepository / AppReadRepository (Database.MsSql implementation)
              → AppDbContext → MSSQL
```

---

## Layer Responsibilities

### Web.BlazorServer — Presentation
- Components inject `IXxxHandler` interfaces (from `Handlers/Repositories/`) only — never `IMediator`, `AppDbContext`, or infrastructure repositories
- `Handlers/Repositories/` contains per-feature handler interfaces that group related commands/queries
- `Handlers/Implementations/` contains the concrete handlers — thin dispatchers that call `IMediator.Send()` and nothing else
- ViewModels are mapped from DTOs inside the component — never returned from Application handlers
- All components inherit `BaseComponent`

### Application.UseCases — Use Cases
- MediatR handlers are the entry point from the Web layer (via `IMediator.Send()`)
- `IXxxService` interfaces are **internal Application contracts** — called by handlers, not by the Web layer
- Mapster profiles live here and are applied inside handlers
- Handlers are organized per type → per feature:
  ```
  Commands/[Feature]/[Action]Cmd.cs    (record + handler co-located in one file)
  Queries/[Feature]/[Action]Qry.cs     (record + handler co-located in one file)
  ```

### Application.DataTransferObjects — DTOs
- DTOs are the data contracts passed between Application handlers and the Web layer
- Never return Domain entities beyond the Application boundary
- No logic or mapping profiles here — Mapster profiles belong in `Application.UseCases`

### Domain.Entities — Business Model
- Full DDD: aggregates, entities, value objects, domain events
- Aggregate roots own all mutations to child entities
- ⚠️ Aggregate root enforcement is currently inconsistent — verify per feature before writing handlers
- References MediatR (`INotification` for domain events) and Ardalis.Guards
- Zero EF Core dependencies

### Database.Libraries — Data Abstractions
- SQL query manager (`ISqlQueryManager`) and EF Core helper abstractions
- No concrete repository implementations here

### Database.MsSql — Concrete Data Access
- `AppDbContext`, EF Core migrations, and all repository implementations
- `IEntityTypeConfiguration<T>` for all entity configs

### Shared.Libraries — Utilities
- Utility/helper classes and C# extension methods only
- No domain logic, no EF Core, no MediatR

### Integration.SAP — SAP HTTP Client
- Outbound SAP API calls only
- Called from Application handlers — never from Web layer directly

---

## What Belongs Where

| Concern | Location |
|---|---|
| UI markup and component logic | `Web.BlazorServer/Components/Pages/` |
| Per-feature handler interfaces (injected into components) | `Web.BlazorServer/Handlers/Repositories/` |
| Per-feature handler implementations (`IMediator.Send()` calls) | `Web.BlazorServer/Handlers/Implementations/` |
| MediatR commands, queries, handlers | `Application.UseCases/Commands/` or `Queries/` |
| Internal Application service contracts | `Application.UseCases/Services/` |
| DTOs | `Application.DataTransferObjects/` |
| Mapster profiles | `Application.UseCases/` |
| Domain entities, aggregates, events | `Domain.Entities/` |
| EF Core configurations | `Database.MsSql/` |
| Migrations | `Database.MsSql/` |
| Generic repository interfaces (`IAppCommandRepository`, `IAppReadRepository`) | `Application.UseCases/Repositories/Bases/` |
| Entity-specific repository interfaces (`IXxxReadRepository`) | `Application.UseCases/Repositories/Domain/` |
| Repository implementations | `Database.MsSql/` |
| SAP HTTP calls | `Integration.SAP/` |
| Helpers and extensions | `Shared.Libraries/` |
