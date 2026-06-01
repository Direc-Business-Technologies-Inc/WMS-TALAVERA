# Blazor Conventions

## Request Flow
Every user interaction in a Blazor component follows this exact pipeline:

```
Blazor Component (inherits BaseComponent)
  → @inject IXxxHandler (Handlers/Repositories/ — per-feature interface)
    → XxxHandler (Handlers/Implementations/ — calls IMediator.Send())
      → Application.UseCases Handler
        → IXxxService (internal Application contracts)
          → Infrastructure Repository → AppDbContext → MSSQL
```

**Never shortcut this chain.** Do not inject `IMediator`, `IAppCommandRepository`, `IAppReadRepository`, `IXxxReadRepository`, `AppDbContext`, or concrete handler implementations directly into components. Always inject the `IXxxHandler` interface.

---

## BaseComponent
All components inherit `BaseComponent` (`/Components/Base/BaseComponent.razor`).

- Provides shared lifecycle methods, common DI, and helpers used across all components
- Every new `.razor` file must inherit `BaseComponent` — no exceptions
- Do not duplicate anything `BaseComponent` already provides in individual components

---

## Project Structure

```
Web.BlazorServer/
├── wwwroot/
│   ├── assets/                  # Static images and assets
│   └── js/
│       └── custom-scripts/      # Page-specific JS (login.js, logout.js)
├── Components/
│   ├── App.razor
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Base/
│   │   └── BaseComponent.razor         # Base class for ALL components
│   ├── Layout/
│   │   └── ProtectedLayout.razor       # Authenticated layout wrapper
│   ├── Pages/
│   │   └── [Feature]/                  # Feature folders containing page components
│   │       └── [Page].razor + .razor.cs
│   ├── Security/                       # Auth controllers, services, policy providers
│   └── Shared/
│       ├── Abstraction/                # Reusable UI abstractions (AppDataGrid, AppBody, etc.)
│       ├── Skeletons/                  # Loading skeleton components
│       ├── Others/                     # Header, Footer, NavigationMenu
│       └── CascadingValues/            # HasUnsavedChangesProvider, LoadingScreenProvider
├── Handlers/
│   ├── Repositories/
│   │   └── [Feature]/                  # Per-feature handler interfaces
│   │       └── IXxxHandler.cs          # Injected into components via @inject
│   └── Implementations/
│       └── [Feature]/                  # Web-layer MediatR dispatchers
│           └── [Feature]Handler.cs     # Calls IMediator.Send() directly
├── Services/
│   ├── Repositories/                   # Web-layer service interfaces
│   │   └── IXxxService.cs
│   └── Implementation/                 # Web-layer service implementations
│       └── XxxService.cs
└── ViewModels/
    └── [Feature]VM.cs                  # Blazor-specific display models
```

---

## Web/Handlers

The handler layer is split into interfaces and implementations:

### Handlers/Repositories/ — Interfaces
Per-feature handler interfaces that group related operations for a specific domain area.

- Injected into Blazor components via `@inject`
- Each interface groups related operations (e.g. all Receiving-related sends)
- Naming: `IXxxHandler.cs` under `/Handlers/Repositories/[Feature]/`
- Do not put business logic here — only declare the operations

### Handlers/Implementations/ — Concrete Dispatchers
Thin implementations that call `IMediator.Send()` with the appropriate command or query.

- Implement the corresponding `IXxxHandler` interface
- Naming: `[Feature]Handler.cs` under `/Handlers/Implementations/[Feature]/`
- No logic beyond constructing the request and calling `IMediator.Send()`

---

## ViewModels
Blazor-specific display models — distinct from DTOs in `Application.DataTransferObjects`.

- ViewModels are UI-concern models (may include `IsSelected`, `IsEditing`, display-formatted fields)
- DTOs are Application-layer data contracts
- Map DTOs → ViewModels inside the component or in a dedicated mapper — never return ViewModels from Application handlers
- Naming: `[Feature]VM.cs` under `/ViewModels/`

---

## Forms & Validation
Custom form handling — no `EditForm` or `DataAnnotationsValidator`.

- Validation logic lives in the component's code-behind (`.razor.cs`)
- Do not introduce `EditForm` without explicit instruction

---

## Code-Behind Convention
Page components use partial class code-behind files:

```
OrderPage.razor       # Markup only
OrderPage.razor.cs    # Logic, lifecycle, DI injections
```

- Keep `.razor` files focused on markup
- All `@inject`, lifecycle methods, and event handlers go in `.razor.cs`

---

## Async Rules
- All data operations must be `async` — never block the SignalR circuit
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- Use `async Task` for all lifecycle methods (`OnInitializedAsync`, etc.)
- Call `StateHasChanged()` only when mutating state outside Blazor's event callback cycle

---

## Layout & Auth
- Protected pages use `ProtectedLayout.razor` as their layout
- Authentication handled via `AppAuthenticationService.cs` in `/Security/`
