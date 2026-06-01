# Domain Layer

## Project
`Domain.Entities/`

## Purpose
The single source of truth for all business models, rules, and domain events in LSMS.
Zero EF Core dependencies — the domain is fully persistence-ignorant.

---

## Project Structure

```
Domain.Entities/
├── Commons/              # Abstract base classes (EntityDEM, AuditableDEM)
├── Entities/             # Concrete domain entities, organized by feature/subdomain
│   ├── Administration/   # User and role entities
│   ├── System/           # System-level entities (e.g. ModuleDEM)
│   └── Transaction/      # Transactional entities
│       └── Common/       # TransactionalDocumentDEM and shared transactional types
├── Enums/                # All system-wide enums (single source of truth)
├── Events/
│   └── Bases/            # DomainBaseEvent — base class for all domain events
├── Extensions/           # Domain-specific extension methods (e.g. DomainEntityExtensions)
├── Markers/              # Tag interfaces for type identification (e.g. IEntity)
├── Providers/            # Concrete provider implementations (e.g. DateTimeProvider)
└── ValueObjects/         # Immutable value objects (e.g. MoneyVO)
```

---

## Naming Convention

| Type | Suffix | Example |
|---|---|---|
| Domain Entity Model | `[Feature]DEM` | `ModuleDEM`, `EntityDEM` |
| Domain Base Event | `DomainBaseEvent` | (base class) |
| Value Object | `[Concept]VO` | `MoneyVO` |
| Marker interface | `I[Concept]` | `IEntity` |
| Provider interface | `I[Concept]Provider` | (in `/Providers/`) |

**DEM = Domain Entity Model.** All domain entity classes carry this suffix.

---

## Base Classes (Commons)

### `EntityDEM`
Root base class. All domain entities inherit from `EntityDEM`, directly or indirectly.

- Carries the domain event collection:
  ```csharp
  public List<DomainBaseEvent> DomainEvents { get; protected set; }
  ```
- Domain events are raised onto this list by the entity and dispatched by the Application layer after persistence.

### `AuditableDEM`
Extends `EntityDEM`. Used for all entities that require audit tracking (created/modified metadata).
All auditable entities inherit from `AuditableDEM` rather than `EntityDEM` directly.

### `TransactionalDocumentDEM`
Located in `Entities/Transaction/Common/TransactionalDocumentDEM.cs` — not in `Commons/`.
Extends `AuditableDEM`. Used as the base for all transactional document entities (e.g. purchase orders, sales orders, or similar business documents).
Provides common document header fields such as document number, date, and status.
Inherit from this when the entity represents a transactional document rather than a plain auditable record.

**Inheritance chain:**
```
EntityDEM
  └── AuditableDEM
        └── TransactionalDocumentDEM
```

---

## Domain Events

### `DomainBaseEvent`
Located in `Events/Bases/DomainBaseEvent.cs`. Inherits MediatR's `INotification`.
All domain events extend `DomainBaseEvent`.

> ⚠️ **Architectural Debt:** The Domain referencing MediatR for `INotification` is a known coupling issue.
> Do not attempt to fix this without explicit instruction. See `architectural_debts.md`.

---

## Enums

The Domain is the **single source of truth** for all transactional and business-domain enums used across the system.

**Exceptions:** Enums scoped to Kernel or Shared processes live in their respective projects and are not duplicated here.

When adding a new enum, always place it in `Domain.Entities/Enums/` unless it is explicitly Kernel- or Shared-scoped.

---

## Markers

Tag interfaces (e.g. `IEntity`) used for type identification and generic constraints.
They carry no members — their presence on a type is their purpose.

---

## Extensions

`DomainEntityExtensions` and related files provide general utility methods for domain entities.
These are domain-scoped helpers — do not add application or infrastructure concerns here.

## Providers

Concrete provider implementations that live directly in the Domain (e.g. `DateTimeProvider`).
These supply domain-level utilities (such as current time) needed by entities or domain logic without introducing external dependencies.

---

## Value Objects

Immutable types that represent domain concepts without identity (e.g. `MoneyVO`).
Equality is based on value, not reference.
Business rules related to the concept belong on the value object itself.

---

## Rules

- Never add EF Core references or data annotations to any Domain class
- Never return Domain entities beyond the Application boundary — always map to a DTO in `Application.UseCases`
- Domain logic belongs on entities and value objects — not in handlers, repositories, or services
- Aggregate roots must own all mutations to their child entities
  - ⚠️ This is currently inconsistently enforced — verify per aggregate before writing handlers (see `architectural_debts.md`)
