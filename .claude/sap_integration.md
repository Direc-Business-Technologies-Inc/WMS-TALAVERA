# SAP Integration

## Location
`Integration.SAP/`

## Pattern
- Outbound HTTP client only — wraps SAP API calls
- Does not call Domain or Application layers directly
- Results are returned to the calling handler and flow through normal application layers
- SQL scripts for SAP data queries live in `Integration.SAP/SQLScripts/`

## Adding a New SAP Endpoint
1. Add the HTTP client method in `Integration.SAP`
2. Add a corresponding MediatR command/query + handler in `Application.UseCases`
3. Expose via the handler's `IXxxIntegration` interface — do not call `Integration.SAP` directly from the Web layer
4. Do not make SAP HTTP calls from any other project

## What Not To Do
- Do not call SAP APIs from handlers or services directly — keep all SAP HTTP logic in `Integration.SAP`
- Do not bypass the handler layer to call `Integration.SAP` from Blazor components
