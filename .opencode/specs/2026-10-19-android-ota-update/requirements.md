# Requirements — Android Over-The-Air APK Update

**Date:** 2026-10-19
**Status:** Confirmed
**Mode:** FEATURE
**Scope:** `Mobile.MAUI` (Android only) + static release hosting on the client IIS server
**Related:** None found

> **Review revision (2026-10-28):** Updated at the user's request to use a fixed IIS-hosted manifest and APK, so mobile-only releases do not require API redeployment. The IIS hostname/path remain deployment configuration decisions.

## Current-state corrections (2026-10-28)

- Polaris operational endpoint initialization is unrelated to OTA discovery. The updater uses a fixed HTTPS manifest URL independent of endpoint settings, so empty `endpoint-settings` does not skip checking and endpoint edits do not change the update host.
- Both the manifest and APK are hosted on a dedicated client IIS site over trusted HTTPS. Confirm certificate validity and network reachability from pilot/target devices before mandatory rollout.
- Restoring an older manifest only removes the update block for devices whose installed version is then at/above the advertised version. It does not downgrade a bad APK already installed. Recovery from a broken installed release requires publishing a corrected, same-key-signed APK with a strictly higher `versionCode`.
- Android `ACTION_VIEW` installer hand-off does not provide a reliable in-process “declined” result. On return/resume, the app must re-check the installed version and remain blocked if it is still below the mandatory target.
- `Api.CoreWebAPI` is not part of the OTA delivery path; IIS serves both static artifacts. This avoids adding public API actions and prevents mobile-only releases from requiring API deployment.
- The release manifest and APK SHALL both be hosted on a dedicated HTTPS IIS site/path reachable by target devices. The mobile app SHALL check a fixed manifest URL independent of the configured operational API endpoint. This avoids API changes/deployments for mobile-only releases.
- IIS static hosting means the update feature adds no anonymous API endpoint. Configure IIS MIME handling, HTTPS certificate, read access, and cache behavior for manifest (revalidate/no-cache) and immutable versioned APK files.

### User stories

- As an operator, I want the app to tell me when a newer mandatory APK is available and guide me through installing it, so I can update at my location without a technician visiting.
- As a warehouse operator, I need the app to remain usable when the update service is unavailable, while still being blocked after a successful check identifies a mandatory newer release.
- As a release operator, I need a pilot and a recoverable release process so an invalid mandatory APK does not strand the fleet.

## Introduction

Today, distributing mobile changes to client devices requires manually building an APK and transferring it to a colleague, who must then physically access a device that is frequently in use. This feature lets a deployed device discover, download, and install a newer APK on demand, and prevents use of an outdated build.

The system **prompts**; it does not install silently. Android requires an explicit user confirmation for package installation by non-privileged apps, and the field devices are not MDM-managed. "Required" therefore means *the app refuses to function until the installed version matches the published version* — enforcement is application-level, not OS-level.

Confirmed operating assumptions: the field fleet is Android-only, one physical device touch is possible per device, publishing is manual, and enforcement is a hard block until updated.

## Glossary

- **Version manifest** — server-published JSON describing the current release (`version`, `versionCode`, `apkUrl`, `sha256`, `releaseNotes`, `mandatory`).
- **`versionCode`** — Android's monotonically increasing integer build identifier (`ApplicationVersion` in `Mobile.MAUI/Mobile.MAUI.csproj:32`). The authoritative comparison key; the display string is cosmetic.
- **Installed version code** — the integer package version code reported by Android for the installed app. The implementation must verify the correct MAUI/Android API that exposes it; a display version string is not authoritative.
- **Mandatory release** — a manifest entry flagged `mandatory: true`; the app blocks until updated.
- **Guided install** — download plus hand-off to the Android system installer, requiring a user tap.
- **Unknown-sources gate** — the per-app Android setting "Install unknown apps", which the user must enable once.
- **Fail open** — when the update check cannot complete, the app proceeds instead of blocking.

## Requirements

### 1. Version discovery

- **1.1** The system SHALL expose a fixed HTTPS URL on the client IIS update site returning the current version manifest for Android.
- **1.2** The manifest SHALL include `versionCode` (integer), `version` (string), `apkUrl`, `sha256`, `releaseNotes`, and `mandatory`.
- **1.3** IIS SHALL serve the manifest with cache headers that require revalidation/prevent stale manifest use; versioned APK files MAY use long-lived immutable caching.
- **1.4** The manifest SHALL be replaceable on IIS without rebuilding or redeploying `Api.CoreWebAPI`.
- **1.5** The mobile app SHALL perform a version check at application start using a fixed, build/deployment-configured update manifest URL independent of `endpoint-settings`; changing operational API endpoint settings SHALL NOT change the update host or trigger an in-session enforcement check.
- **1.6** WHEN a startup version check fails due to network, server, TLS, or invalid-manifest failure, THEN the app SHALL proceed and SHALL NOT block. If a mandatory gate is already active and a recheck fails after installer return, the app SHALL remain gated with a retry path until a successful check confirms the target is met or a valid manifest removes the requirement.

### 2. Version comparison and enforcement

- **2.1** The app SHALL compare installed `versionCode` to manifest `versionCode` numerically.
- **2.2** WHEN a successful manifest check reports `versionCode` greater than installed `versionCode` AND `mandatory` is `true`, THEN the app SHALL present a non-dismissible update screen and SHALL NOT allow access to any WMS functionality. The block SHALL persist across app restarts until a successful check reports the installed `versionCode` is greater than or equal to the manifest `versionCode`. A startup check failure follows Req 1.6; a failed recheck while returning from installer SHALL retain the active gate.
- **2.3** WHEN the manifest `versionCode` is greater than the installed `versionCode` AND `mandatory` is `false`, THEN the app SHALL present a dismissible prompt offering Update or Later.
- **2.4** WHEN the manifest `versionCode` is equal to or lower than the installed `versionCode`, THEN the app SHALL proceed normally with no prompt.
- **2.5** The app SHALL NOT interrupt an in-progress session to enforce an update; enforcement SHALL occur only at application start or after an explicit user action.
- **2.6** The enforcement state SHALL be re-evaluated on every application start.
- **2.7** The release operator SHALL be able to replace the published manifest on IIS without rebuilding or redeploying API code. Reverting it to a lower `versionCode` SHALL remove the block on devices whose installed code is at least that value, but SHALL NOT downgrade installed APKs. Recovery from a defective installed release SHALL publish a corrected APK with a higher `versionCode` and the same signing identity.

### 3. Download and integrity

- **3.1** The app SHALL download the APK to app-private storage.
- **3.2** The app SHALL compute the SHA-256 of the downloaded file and compare it to the manifest `sha256`.
- **3.3** WHEN the computed hash does not match, THEN the app SHALL discard the file, report a failure, and SHALL NOT offer installation.
- **3.4** The app SHALL display download progress and SHALL allow cancellation.
- **3.5** The app SHALL reject manifest or `apkUrl` requests that are not HTTPS, have untrusted certificates, or redirect from HTTPS to HTTP. Before mandatory rollout, the release operator SHALL verify target-device reachability.
- **3.6** The app SHALL tolerate a failed or partial download without corrupting existing application state.

### 4. Installation hand-off (Android)

- **4.1** The app SHALL hand the verified APK to the Android system package installer via a `content://` URI.
- **4.2** The app SHALL verify whether the unknown-sources gate is enabled before attempting installation.
- **4.3** WHEN the gate is disabled, THEN the app SHALL direct the user to the correct system settings screen and SHALL explain why it is needed.
- **4.4** WHEN the operator returns to the app without installing the requested APK, THEN the app SHALL re-check the installed version and return to the blocking screen while the mandatory update remains pending. The app SHALL NOT depend on Android returning a reliable decline result from the installer intent.
- **4.5** The app SHALL NOT attempt silent or privileged installation.

### 5. Publishing and release discipline

- **5.1** The manifest `versionCode` SHALL be derived from the built APK, not entered independently.
- **5.2** The manifest `sha256` SHALL be computed from the exact APK being published.
- **5.3** The release operator SHALL manually increment `ApplicationVersion` for every published release; no automatic increment mechanism is in scope.
- **5.4** Every published APK SHALL be signed with the same release keystore.
- **5.5** The release process SHALL support a staging IIS location/manifest that pilot devices can target before fleet-wide release.
- **5.6** Before publishing, the operator SHALL be able to verify that the APK installs successfully on at least one device and that its `versionCode` and `sha256` match the manifest.
- **5.7** Publishing SHALL require no more than: build APK, run one documented command that extracts its package version/versionCode, computes its SHA-256, and generates the manifest, then copy the versioned APK and manifest to IIS. No CI/CD pipeline is required. This workflow SHALL NOT include API deployment for mobile-only changes.

### 6. Security

- **6.1** The update site SHALL expose only the intended read-only manifest and versioned APK files; this feature SHALL add no anonymous API endpoints.
- **6.2** The manifest and APK SHALL be served over HTTPS with trusted certificates.
- **6.3** The system SHALL NOT treat the manifest `sha256` as a trust boundary; installation trust SHALL derive from Android's APK signature verification.
- **6.4** The feature SHALL NOT weaken existing authentication or authorization for any other endpoint.

### 7. Diagnostics

- **7.1** The app SHALL record the outcome of each update check (up-to-date, update available, check failed) locally.
- **7.2** The app SHALL surface a human-readable reason when an update cannot be completed.
- **7.3** The app SHALL display the target version and a support contact/instruction on the blocking screen, so a stuck user can escalate without guessing.

### 8. Platform scope

- **8.1** The update mechanism SHALL be implemented for Android only.
- **8.2** On non-Android targets, the update check SHALL be inert and SHALL NOT affect startup.

### 9. Update-site contract and release recovery

- **9.1** The update service SHALL request a fixed HTTPS manifest URL supplied by non-secret mobile build/deployment configuration independently of operational API endpoint settings.
- **9.2** The manifest SHALL contain an absolute HTTPS URL to a versioned APK hosted on the IIS update site. Both manifest and APK SHALL be available from that site.
- **9.3** The publishing instructions SHALL distinguish manifest rollback (unblock compatible installed versions) from APK repair (publish a higher-version corrected APK signed by the same release key).

## In Scope

- Fixed HTTPS static hosting of manifest and APK on client IIS; no `Api.CoreWebAPI` changes are required for a mobile-only release.
- Native download, hash verification, and installer hand-off in `Mobile.MAUI` (Android).
- Start-up enforcement UI in the Blazor Hybrid layer.
- Manifest generation from the built APK artifact and publication to IIS.

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
- **P6** App startup SHALL proceed within the configured bounded update-check timeout when the update site is unreachable.
- **P7** Update checking SHALL use the fixed update URL regardless of saved operational endpoint settings or Polaris default initialization; startup ordering SHALL not depend on `CleanLayout` being rendered before the update gate.
- **P8** Recent scan-first transaction selection, fresh quantity validation, and other existing mobile/API operational behaviors SHALL remain unchanged by the updater.
- **P9** Publishing a mobile-only release SHALL NOT require rebuilding or redeploying `Api.CoreWebAPI`.
