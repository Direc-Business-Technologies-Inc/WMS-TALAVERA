# Requirements — Android Over-The-Air APK Update

**Date:** 2026-10-19
**Status:** Confirmed
**Mode:** FEATURE
**Scope:** `Mobile.MAUI` (Android only) + `Api.CoreWebAPI`
**Related:** None found

## Introduction

Today, distributing mobile changes to client devices requires manually building an APK and transferring it to a colleague, who must then physically access a device that is frequently in use. This feature lets a deployed device discover, download, and install a newer APK on demand, and prevents use of an outdated build.

The system **prompts**; it does not install silently. Android requires an explicit user confirmation for package installation by non-privileged apps, and the field devices are not MDM-managed. "Required" therefore means *the app refuses to function until the installed version matches the published version* — enforcement is application-level, not OS-level.

Confirmed operating assumptions: the field fleet is Android-only, one physical device touch is possible per device, publishing is manual, and enforcement is a hard block until updated.

## Glossary

- **Version manifest** — server-published JSON describing the current release (`version`, `versionCode`, `apkUrl`, `sha256`, `releaseNotes`, `mandatory`).
- **`versionCode`** — Android's monotonically increasing integer build identifier (`ApplicationVersion` in `Mobile.MAUI/Mobile.MAUI.csproj:32`). The authoritative comparison key; the display string is cosmetic.
- **Installed version** — the value reported by `AppInfo.Current.BuildString` on the device.
- **Mandatory release** — a manifest entry flagged `mandatory: true`; the app blocks until updated.
- **Guided install** — download plus hand-off to the Android system installer, requiring a user tap.
- **Unknown-sources gate** — the per-app Android setting "Install unknown apps", which the user must enable once.
- **Fail open** — when the update check cannot complete, the app proceeds instead of blocking.

## Requirements

### 1. Version discovery

- **1.1** The system SHALL expose an anonymously accessible endpoint returning the current version manifest for a given platform.
- **1.2** The manifest SHALL include `versionCode` (integer), `version` (string), `apkUrl`, `sha256`, `releaseNotes`, and `mandatory`.
- **1.3** The endpoint SHALL respond with cache headers that prevent a device or intermediary proxy from serving a stale manifest.
- **1.4** The manifest SHALL be updatable without rebuilding or redeploying the API.
- **1.5** The mobile app SHALL perform a version check at application start and after endpoint settings change.
- **1.6** WHEN the version check fails due to network or server unavailability, THEN the app SHALL proceed and SHALL NOT block, even if a previous check reported a mandatory update. A server outage SHALL never prevent warehouse operations.

### 2. Version comparison and enforcement

- **2.1** The app SHALL compare installed `versionCode` to manifest `versionCode` numerically.
- **2.2** WHEN the manifest `versionCode` is greater than the installed `versionCode` AND `mandatory` is `true`, THEN the app SHALL present a non-dismissible update screen and SHALL NOT allow access to any WMS functionality. The block SHALL persist across app restarts until the installed `versionCode` is greater than or equal to the manifest `versionCode`.
- **2.3** WHEN the manifest `versionCode` is greater than the installed `versionCode` AND `mandatory` is `false`, THEN the app SHALL present a dismissible prompt offering Update or Later.
- **2.4** WHEN the manifest `versionCode` is equal to or lower than the installed `versionCode`, THEN the app SHALL proceed normally with no prompt.
- **2.5** The app SHALL NOT interrupt an in-progress session to enforce an update; enforcement SHALL occur only at application start or after an explicit user action.
- **2.6** The enforcement state SHALL be re-evaluated on every application start.
- **2.7** The operator SHALL be able to revert the published manifest to a previous `versionCode` without rebuilding or redeploying the API, and devices SHALL accept the reverted manifest on next start.

### 3. Download and integrity

- **3.1** The app SHALL download the APK to app-private storage.
- **3.2** The app SHALL compute the SHA-256 of the downloaded file and compare it to the manifest `sha256`.
- **3.3** WHEN the computed hash does not match, THEN the app SHALL discard the file, report a failure, and SHALL NOT offer installation.
- **3.4** The app SHALL display download progress and SHALL allow cancellation.
- **3.5** The app SHALL reject an `apkUrl` whose scheme is not HTTPS, regardless of the configured endpoint protocol.
- **3.6** The app SHALL tolerate a failed or partial download without corrupting existing application state.

### 4. Installation hand-off (Android)

- **4.1** The app SHALL hand the verified APK to the Android system package installer via a `content://` URI.
- **4.2** The app SHALL verify whether the unknown-sources gate is enabled before attempting installation.
- **4.3** WHEN the gate is disabled, THEN the app SHALL direct the user to the correct system settings screen and SHALL explain why it is needed.
- **4.4** WHEN the user declines the system install prompt, THEN the app SHALL return to the update screen with the update still pending.
- **4.5** The app SHALL NOT attempt silent or privileged installation.

### 5. Publishing and release discipline

- **5.1** The manifest `versionCode` SHALL be derived from the built APK, not entered independently.
- **5.2** The manifest `sha256` SHALL be computed from the exact APK being published.
- **5.3** The build process SHALL guarantee that `ApplicationVersion` increments on every published release.
- **5.4** Every published APK SHALL be signed with the same release keystore.
- **5.5** The system SHALL support publishing to a staging location that specific devices can target before fleet-wide release.
- **5.6** Before publishing, the operator SHALL be able to verify that the APK installs successfully on at least one device and that its `versionCode` and `sha256` match the manifest.
- **5.7** Publishing SHALL require no more than: build APK, run one documented command to generate the manifest, copy both to the host. No CI/CD pipeline is required.

### 6. Security

- **6.1** The manifest endpoint SHALL be the only anonymous endpoint added by this feature.
- **6.2** The APK SHALL be served over HTTPS.
- **6.3** The system SHALL NOT treat the manifest `sha256` as a trust boundary; installation trust SHALL derive from Android's APK signature verification.
- **6.4** The feature SHALL NOT weaken existing authentication or authorization for any other endpoint.

### 7. Diagnostics

- **7.1** The app SHALL record the outcome of each update check (up-to-date, update available, check failed) locally.
- **7.2** The app SHALL surface a human-readable reason when an update cannot be completed.
- **7.3** The app SHALL display the target version and a support contact/instruction on the blocking screen, so a stuck user can escalate without guessing.

### 8. Platform scope

- **8.1** The update mechanism SHALL be implemented for Android only.
- **8.2** On non-Android targets, the update check SHALL be inert and SHALL NOT affect startup.

## In Scope

- Anonymous version-manifest endpoint in `Api.CoreWebAPI`.
- APK delivery over HTTPS.
- Native download, hash verification, and installer hand-off in `Mobile.MAUI` (Android).
- Start-up enforcement UI in the Blazor Hybrid layer.
- Manifest generation tied to the build.

## Out of Scope

- Silent, OS-level, or forced installation (requires device-owner or MDM).
- Delta/patch/incremental updates.
- iOS, MacCatalyst, and Windows update paths.
- MDM enrolment, device provisioning, or fleet management.
- In-app rollback to a previous version.
- An admin UI for publishing releases.
- Changing `usesCleartextTraffic` or the general endpoint protocol policy.

## Preservation Requirements

- **P1** Existing login, authentication, and token-refresh behavior SHALL be unchanged (`Components/Pages/Login/Login.razor.cs:27`, `Services/JWTAuthStateProvider.cs`).
- **P2** The `endpoint-settings` SecureStorage contract and `ClientEndpointVM` shape SHALL remain backward compatible (`ViewModel/ClientEndpointVM.cs`, `Components/Pages/Administrator/EditEndpointDialog.razor.cs:37`).
- **P3** `ApiClientService` behavior, including its 401 handling (`Services/ApiClientService.cs:76-80`), SHALL be unchanged; the update service SHALL NOT reuse it.
- **P4** The existing scanner broadcast handling SHALL be unaffected (`Platforms/Android/MainActivity.cs:23-41`, `Platforms/Android/Helper/ScanReceiver.cs`).
- **P5** All existing API routes and their authentication requirements SHALL remain unchanged.
- **P6** App startup SHALL NOT be measurably slower when the update server is unreachable.
