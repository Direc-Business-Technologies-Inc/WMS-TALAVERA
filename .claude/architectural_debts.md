# Architectural Debts

These are known issues in the codebase. **Do not fix any of these without explicit instruction from the developer.**

---

## 1. Domain references MediatR
**Location:** `Domain.Entities/`
**Issue:** `Domain.Entities` references MediatR solely for `INotification` on domain events. This couples the pure domain model to an infrastructure library.
**Correct fix (when instructed):** Define a local `IDomainEvent` interface in `Domain.Entities` and adapt it to MediatR's `INotification` in `Application.UseCases`.

---

## 2. Aggregate root enforcement is inconsistent
**Location:** `Domain.Entities/`
**Issue:** Child entities are sometimes modified directly from outside the aggregate root, bypassing DDD invariant protection.
**Impact:** Do not assume any aggregate's invariants are enforced. Verify per feature before writing handlers.

---

## 3. Generic + specific repositories coexist
**Location:** `Database.MsSql/`
**Issue:** Both `IAppCommandRepository`/`IAppReadRepository` (generic) and `IXxxReadRepository` (entity-specific) are in use with no strict rule on which to prefer.
**Current rule:** Prefer entity-specific repositories for complex queries; generic for simple CRUD. Do not consolidate without instruction.

---

## 4. Mixed error handling
**Location:** `Application.UseCases/`
**Issue:** No consistent strategy — some handlers throw exceptions for expected failures, others return value types.
**Current rule:** Expected failures → return value (`null`, `bool`, result wrapper). Unexpected/system errors → throw. Apply this going forward; do not retroactively unify old handlers without instruction.

---

## 5. MediatR dispatch coordination lives in the presentation layer
**Location:** `Web.BlazorServer/Handlers/Repositories/`, `Web.BlazorServer/Handlers/Implementations/`
**Issue:** MediatR dispatch coordination sits in the Web project rather than the Application layer. Handler interfaces live in `Handlers/Repositories/` and their concrete dispatchers in `Handlers/Implementations/`. This is the established and intentional pattern for this codebase.
**Current rule:** All new features follow this pattern. Do not move handler interfaces or implementations to the Application layer without explicit instruction.
