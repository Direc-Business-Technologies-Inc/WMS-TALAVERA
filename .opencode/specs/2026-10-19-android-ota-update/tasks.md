# Tasks — Android Over-The-Air APK Update

**Date:** 2026-10-19
**Status:** Confirmed
**Mode:** FEATURE
**Spec:** [design.md](design.md)
**Goal:** Deliver prompted Android APK updates through a fixed HTTPS IIS update site, without requiring API deployment for mobile-only releases.

> **Review revision (2026-10-28):** Replaced the API-hosted update plan with IIS static hosting for both `latest.json` and versioned APKs. No automatic version-increment feature is included.

## Verified technical context and constraints

- `Mobile.MAUI` targets `net9.0-android`, minimum Android API 24 (`Mobile.MAUI/Mobile.MAUI.csproj:4,39`). `ApplicationDisplayVersion` and `ApplicationVersion` are at lines 31-32.
- MAUI documents Android `AppInfo.Current.BuildString` as `android:versionCode`; still verify it against a built APK on a pilot device.
- Root component path is `MainPage.xaml:9-12` → `Components/Routes.razor:3-16`. Preserve the existing authentication cascade and Router.
- Operational endpoint settings (`endpoint-settings`) and Polaris default are unrelated to the update host. Fixed update URL must not depend on `EndpointHelper`, `EndpointDefaults`, or API endpoint changes.
- Do not modify `ApiClientService`, endpoint settings contract, authentication, scanner behavior, or existing API routes. No `Api.CoreWebAPI` source or deployment changes are needed for OTA.
- Keep same release signing identity for every upgrade; increase `ApplicationVersion` for each published APK. No auto-increment logic is part of this plan.
- Startup check failure is fail-open. If installer-return recheck fails while the app already has a mandatory gate active, retain the gate and provide retry.
- Do not create a test project; no branching, committing, or pushing as part of this specification.

## Phase 1 — IIS update-site contract and release configuration

### Task 1.1: Confirm IIS update-site deployment settings
`_Requirements: 1.1, 1.3, 1.4, 5.5, 6.1, 6.2, 9.1, 9.2_`

- Agree with the client on production and staging HTTPS URLs, for example `https://updates.client.example.com/android/latest.json` and `/android/apk/{version}.apk`.
- Configure a trusted TLS certificate, read-only IIS identity/ACL, no directory browsing, `.apk` MIME type `application/vnd.android.package-archive`, and manifest revalidation/no-cache headers. Versioned APKs may be long-lived immutable cached.
- Keep release files in a persistent IIS directory, separate from API publish output. Confirm deploying `Api.CoreWebAPI` cannot overwrite/delete these files.
- **Done when:** pilot device and workstation can fetch the manifest and APK over trusted HTTPS; IIS serves intended files only with expected MIME/cache behavior.

### Task 1.2: Define manifest schema and fixed URL configuration
`_Requirements: 1.2, 1.5, 9.1, 9.2_`

- Document plain JSON `latest.json` fields: `Version`, `VersionCode`, `ApkUrl`, `Sha256`, `ReleaseNotes`, `Mandatory`.
- Add a non-secret fixed manifest URL setting for the Android mobile build/deployment. Use separate staging/production configuration as required; never derive it from `endpoint-settings` or API URL.
- Require client validation of HTTPS, trusted certificate, no HTTPS-to-HTTP redirect, positive `VersionCode`, non-empty version, 64-hex SHA-256, absolute HTTPS `ApkUrl`, and valid JSON. Invalid response is a failed check, not an up-to-date result.
- **Done when:** documented example manifest passes validation and staging/production URL selection is explicit.

## Phase 2 — Mobile update service

### Task 2.1: Implement `AppUpdateService`
`_Requirements: 1.5, 1.6, 2.1, 2.6, P6, P9_`

- Add `Mobile.MAUI/Services/AppUpdateService.cs`; register via `Services/Extensions.cs`.
- Fetch fixed `latest.json` with bounded timeout and cache revalidation. Use normal certificate validation; reject downgrade redirects. Do not reuse `ApiClientService`.
- Read installed version code safely from the Android MAUI app info value and compare numerically with manifest `VersionCode`.
- Return `UpToDate`, `Optional`, `Mandatory`, or `CheckFailed`; startup exceptions/invalid responses fail open and never escape into app startup.
- Operational API endpoint changes must not alter update URL or trigger a mid-session enforcement check.
- **Done when:** service handles current/newer/optional/mandatory, invalid manifest, offline, timeout, and altered API endpoint cases as specified.

### Task 2.2: Implement download, integrity, and diagnostics
`_Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 7.1, 7.2_`

- Download to app-private cache with progress and cancellation; accept only absolute HTTPS APK URLs and prevent redirect downgrade.
- Calculate SHA-256 for downloaded bytes; delete partial/cancelled/mismatched files; never offer install unless hash matches.
- Record last check result/time locally using `Preferences`; provide readable error text.
- **Done when:** valid download hash matches; modified hash, HTTP URL, redirect downgrade, cancellation, and network failure do not offer installation and leave no partial file.

## Phase 3 — Update gate and Android installation

### Task 3.1: Add root update gate
`_Requirements: 2.2, 2.3, 2.4, 2.5, 2.6, 7.2, 7.3, 8.2_`

- Add `Mobile.MAUI/Components/UpdateGate.razor` and code-behind; wrap the existing Router in `Components/Routes.razor` with `ChildContent`.
- Preserve `CascadingAuthenticationState`, `AuthorizeRouteView`, `DefaultLayout`, and `FocusOnNavigate` unchanged.
- `Ready` renders child Router; mandatory update renders non-dismissible block and support guidance; optional update allows Later; download state shows progress/cancel.
- Startup check runs once per app start. No timer or network-change subscription. Do not block an active session for a release published mid-session.
- **Done when:** mandatory gate hides every route; optional Later and up-to-date/fail-open startup render existing router unchanged.

### Task 3.2: Add Android FileProvider and install hand-off
`_Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_`

- Resolve actual runtime package name before configuring provider authority; note discrepancy between `Platforms/Android/AndroidManifest.xml` and csproj `ApplicationId`.
- Add `REQUEST_INSTALL_PACKAGES`, non-exported FileProvider, and `Platforms/Android/Resources/xml/file_paths.xml` exposing only cache path.
- Verify/install-source settings behavior on Android API 24-25 and API 26+; do not assume older versions expose the per-app API.
- Open verified APK through a `content://` URI and Android installer confirmation; never silently install.
- On app resume, re-read installed version. If a mandatory gate is active and the installer was cancelled or the recheck fails, retain the gate with retry; clear only after a successful check confirms the requirement is met or no longer required.
- **Done when:** signed pilot APK update installs over existing app; returning without installing retains mandatory gate; settings prompt works on supported OS versions.

## Phase 4 — Release runbook and pilot

### Task 4.1: Document manual version discipline and artifact-derived manifest generation
`_Requirements: 5.1, 5.2, 5.3, 5.4, 5.7, 2.7, 9.3_`

- Document that the operator manually increments `ApplicationVersion` for every published release; do not add auto-increment tooling.
- Provide one documented command/script that takes the built signed APK, extracts its display version and Android version code, calculates SHA-256, and generates `latest.json` using release notes, mandatory flag, and the configured HTTPS APK URL as inputs. It must validate that required values are present and match the artifact.
- Publish process: build signed Release APK; generate manifest from that exact artifact; upload APK under immutable versioned filename; upload `latest.json` last; pilot as optional; after successful pilot, replace manifest with `Mandatory: true` if required.
- Keep old APKs and manifest copies for rollback. Explain that manifest rollback cannot downgrade installed APKs; repair requires higher-version same-key APK.
- **Done when:** another operator can follow the runbook and the generated manifest hash/version code match the exact APK.

### Task 4.2: Execute pilot and regression checks
`_Requirements: 5.5, 5.6, P1, P2, P3, P4, P5, P6, P7, P8, P9_`

- Test fresh install, up-to-date, optional update/Later, mandatory update, offline startup, malformed manifest, download failure, bad hash, installer cancellation/resume, and repair APK.
- Verify same-key signed APK installs over current pilot APK without uninstall; confirm runtime version code, package identity, HTTPS trust, progress, and recovery process.
- Verify API deployment is not required and cannot alter IIS update files; changing operational API endpoint does not change updater URL.
- Regression-check login/token refresh, endpoint settings, 401 handling, scanner broadcasts, existing API routes, scan-first workflows, and fresh quantity validation.
- **Done when:** device and IIS checks pass and results are recorded before mandatory fleet rollout.

## Traceability

| Requirement | Tasks |
|---|---|
| 1.1-1.4 | 1.1, 1.2 |
| 1.5-1.6 | 2.1 |
| 2.1-2.7 | 2.1, 3.1, 3.2, 4.1 |
| 3.1-3.6 | 2.2, 3.2 |
| 4.1-4.5 | 3.2 |
| 5.1-5.7 | 1.1, 1.2, 4.1, 4.2 |
| 6.1-6.4 | 1.1, 1.2, 2.2 |
| 7.1-7.3 | 2.2, 3.1 |
| 8.1-8.2 | 2.1, 3.1 |
| 9.1-9.3 | 1.1, 1.2, 4.1 |
| P1-P9 | 2.1, 3.1, 4.2 |

## Build verification

```
dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android
```

No API build/deployment step is required for an OTA mobile-only release. API build/deployment remains part of the normal workflow only when API code itself changes. No test command is listed because no test project covers this feature.
