# Code Conventions

## Naming
| Type | Convention | Example |
|---|---|---|
| Service interface | `IXxxService` | `IUserService` |
| Service implementation | `XxxService` | `UserService` |
| Repository interface (entity-specific) | `IXxxReadRepository` | `IUserReadRepository` |
| Repository implementation (entity-specific) | `XxxReadRepository` | `UserReadRepository` |
| MediatR command | `XxxCmd` | `CreateRoleCmd` |
| MediatR query | `XxxQry` | `GetAllRolesQry` |
| MediatR handler | `XxxCmdHandler` / `XxxQryHandler` | `CreateRoleCmdHandler` |
| DTO | `XxxDTO` | `UserDTO` |
| Blazor page/component | `XxxPage` / `XxxComponent` | `UserListPage` |

## General Rules
- One class per file; filename must match class name
- Constructor injection for all dependencies — no service locator pattern
- `async/await` throughout — never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- Interfaces for all services and repositories
- Never use `dynamic` or anonymous types across layer boundaries

## Error Handling
- **System / unexpected errors** → throw an exception, let global middleware handle it
- **Expected domain failures** → return a value type (`null`, `bool`, or a result wrapper) — do not throw
- Do not use exceptions for control flow on known business cases (e.g. "record not found" is a return value, not an exception)

## What Never To Do
- No `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` anywhere in the codebase
- No raw SQL with string interpolation — always use parameterized inputs
- No Domain entities returned across layer boundaries — always map to a DTO first
- No hardcoded secrets, connection strings, or auth keys anywhere in code or config files
