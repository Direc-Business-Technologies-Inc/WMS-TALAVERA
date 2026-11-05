# Web Form Submit Feedback and Duplicate-Submit Prevention

**Date:** 2026-10-29  
**Status:** Implemented  
**Scope:** Web.BlazorServer form submission and save actions  
**Related:** None found

## Summary

Web form submissions use a per-component `SubmitOnce` guard. The guard is acquired synchronously before awaiting the operation, preventing repeated clicks, keyboard form submissions, and concurrent callback invocations from starting duplicate work. Submit buttons become disabled and display `Saving...` (or a contextual equivalent) while the guarded callback runs.

## Behavior

- The first valid submit starts the action and immediately marks the component as submitting.
- Further submit attempts while that action is pending are ignored.
- The submit button is disabled and its label indicates progress.
- The guard is released in a `finally` block, including after exceptions and validation/cancellation returns, allowing correction and retry.
- Existing business validation and success navigation/toasts remain owned by each form's existing handler.
- Existing `IsBusy` and disabled states are preserved and combined with the submit guard where applicable.

## Implementation

- `Web.BlazorServer/Components/Base/SubmitOnce.cs` provides an atomic, reusable asynchronous submission guard.
- `BaseForm<TItem>` exposes the guard and label helper to inherited pages.
- Standalone forms with their own submit callback use a component-local guard so prevention happens in the handler, not only in button markup.
- Button labels retain the original idle action text and switch to a progress label while submitting.

## Validation

Run `dotnet build Web.BlazorServer/Web.BlazorServer.csproj --no-restore` from the repository root. This change affects only web UI state and does not alter API or database contracts.
