# Tasks — Android Over-The-Air APK Update

**Date:** 2026-10-19
**Status:** Confirmed
**Mode:** FEATURE
**Spec:** [design.md](design.md) (sibling)
**Goal:** Ship prompted, enforced APK updates to Android field devices without MDM, without a CI pipeline, and without breaking existing login, scanning, or API behavior.
**Architecture summary:** One new anonymous controller plus a manifest file in `Api.CoreWebAPI`; one new shared service, one gate component, and one Android partial in `Mobile.MAUI`. No changes to authentication, existing controllers, `ApiClientService`, `ClientEndpointVM`, scanner handling, or routing behavior.

## Verified technical context

- API targets `net9.0` (`Api.CoreWebAPI/Api.CoreWebAPI.csproj:4`); mobile targets `net9.0-android` with min SDK 24 (`Mobile.MAUI/Mobile.MAUI.csproj:4,39`).
- JSON is PascalCase on the wire (`Api.CoreWebAPI/Registers/WebApplicationService.cs:35-38`); the mobile client deserializes with default options (`Services/ApiClientService.cs:82`). Manifest DTOs must use PascalCase.
- Controller convention: `[ApiController]`, `[Route("api/[controller]")]`, `ApiResult<T>` (`Controllers/Lookup/LookupController.cs:12-14`).
- Mobile DI extension point: `Services/Extensions.cs:10-29`; singleton registration pattern at line 20.
- Settings source: `endpoint-settings` in `SecureStorage`; URL built by `Helpers/EndpointHelper.BuildURI` (`EndpointHelper.cs:8-15`).
- Router root: `MainPage.xaml:9-13` → `Components/Routes.razor:4-16`.
- `Platforms/Android/Resources/xml/` does not exist — only `values/colors.xml`.
- No FileProvider, no `FileSystem.*`, and no `PackageManager` usage exists in the mobile project today.
- No API or MAUI test project exists (`WMS.slnx:36-38`). Verification is scriptable HTTP checks plus a manual device matrix.

## Global constraints

- Do not modify `ApiClientService`, `ClientEndpointVM`, `EndpointHelper`, `MainLayout.razor.cs`, or any existing controller.
- Do not touch `usesCleartextTraffic` (`Platforms/Android/AndroidManifest.xml:3`).
- Every new API action gets an explicit `[AllowAnonymous]` even though authorization is currently unenforced.
- The update path must never throw into app startup.
- Do not create a test project; do not invent expected test counts.
- No branching, committing, or pushing as part of this specification.

---

## Phase 1 — Manifest contract and DTO

### Task 1.1: Create the update DTO
`_Requirements: 1.2_`

- File: `Application.DataTransferObjects/Mobile/MobileUpdateVM.cs` (new).
- Add `Version` (string), `VersionCode` (int), `ApkUrl` (string), `Sha256` (string), `ReleaseNotes` (string), `Mandatory` (bool).
- PascalCase property names; no attributes required.
- **Done when:** project compiles and property names match the manifest file exactly.

### Task 1.2: Create the manifest file
`_Requirements: 1.2, 1.4_`

- File: `Api.CoreWebAPI/MobileUpdate/manifest.json` (new).
- Add a JSON object with the six fields from Task 1.1. Use a placeholder `Sha256` of 64 hex characters and a `VersionCode` matching the current `ApplicationVersion` (`Mobile.MAUI.csproj:32`, currently `1`).
- **Done when:** file parses as JSON and its keys match `MobileUpdateVM` exactly.

### Task 1.3: Add configuration keys
`_Requirements: 1.4, 5.7_`

- File: `Api.CoreWebAPI/appsettings.json:1-14`.
- Add a `MobileUpdate` section with `ManifestPath` (default `MobileUpdate/manifest.json`) and `ApkDirectory` (default `MobileUpdate/apk`). Relative paths resolve against the content root. Do not add secrets.
- **Done when:** the section is present and readable via `IConfiguration`.

### Task 1.4: Ensure the manifest is deployed
`_Requirements: 1.4, 5.7_`

- File: `Api.CoreWebAPI/Api.CoreWebAPI.csproj`.
- Add an `ItemGroup` so `MobileUpdate/manifest.json` copies to output with `CopyToOutputDirectory=PreserveNewest`.
- Do **not** copy the APK directory — APKs are placed on the host, not baked into the build.
- **Done when:** `dotnet build` produces the manifest next to the assembly.

**Checkpoint 1:** `dotnet build Api.CoreWebAPI/Api.CoreWebAPI.csproj` succeeds and the manifest is present in the output directory.

---

## Phase 2 — API endpoints

### Task 2.1: Create `MobileUpdateController`
`_Requirements: 1.1_`

- File: `Api.CoreWebAPI/Controllers/Mobile/MobileUpdateController.cs` (new).
- Class with `[ApiController]`, `[Route("api/[controller]")]`, matching `LookupController.cs:12-14`. Inject `IConfiguration`.
- **Done when:** the controller is discovered by `MapControllers()` (`Program.cs:38`).

### Task 2.2: Implement `Latest`
`_Requirements: 1.1, 1.2, 1.3, 1.4, 6.1_`

- `[AllowAnonymous] [HttpGet("Latest")]` returning `ApiResult<MobileUpdateVM>`.
- Read the manifest file at the configured path. On missing file, unreadable file, or deserialization failure, return `ApiResult<MobileUpdateVM>.Failed("...")` with a non-sensitive message.
- Rewrite `ApkUrl` to an absolute URL built from the current request (`Request.Scheme`, `Request.Host`), preserving the configured path segment.
- Set `Cache-Control: no-store, no-cache, must-revalidate` on the response.
- **Done when:** a request returns the manifest, correct headers, and `Success: true`.

### Task 2.3: Implement `Apk/{version}`
`_Requirements: 5.2, 6.2_`

- `[AllowAnonymous] [HttpGet("Apk/{version}")]` returning `FileStreamResult`.
- Validate `version` against a strict digits-and-dots pattern and reject anything else with 400 **before** touching the filesystem.
- Resolve the filename inside the configured APK directory and verify the resolved absolute path is still inside that directory (defeats `..` traversal).
- Return 404 when the file is absent.
- `enableRangeProcessing: true`.
- Set `Cache-Control: public, max-age=31536000, immutable`.
- Content type `application/vnd.android.package-archive`.
- **Done when:** a valid version streams the file, an unknown version 404s, and `Apk/../../appsettings.json` returns 400.

**Checkpoint 2 (scriptable, no device):**

```
dotnet build Api.CoreWebAPI/Api.CoreWebAPI.csproj
curl -i https://<host>/api/MobileUpdate/Latest
curl -i -o apk.bin https://<host>/api/MobileUpdate/Apk/1.0.5
(Get-FileHash apk.bin -Algorithm SHA256).Hash.ToLowerInvariant()
curl -i https://<host>/api/MobileUpdate/Apk/..%2f..%2fappsettings.json
```

Assert: `no-store` on `Latest`; hash matches the manifest; traversal returns 400; unknown version returns 404.

---

## Phase 3 — Mobile shared service

### Task 3.1: Create `AppUpdateService`
`_Requirements: 1.5, 1.6, 2.1, 2.6, P2, P6_`

- File: `Mobile.MAUI/Services/AppUpdateService.cs` (new).
- Private `HttpClient` with a short timeout (e.g. 5 seconds).
- Public async method returning one of: `NoSettings`, `UpToDate`, `Optional`, `Mandatory`, `CheckFailed`.
- Resolve the base URL **per check** (not in the constructor): read `endpoint-settings` from `SecureStorage`, deserialize to `ClientEndpointVM`, call `EndpointHelper.BuildURI`. If absent or empty, return `NoSettings` without any network call.
- GET `api/MobileUpdate/Latest?platform=android`.
- Deserialize with `JsonSerializerOptions` matching the API's PascalCase (`PropertyNamingPolicy = null`).
- Compare `int.Parse(AppInfo.Current.BuildString)` to `VersionCode` numerically.
- Wrap everything in a try/catch that returns `CheckFailed`. Nothing may propagate to the caller.
- **Done when:** with no settings, the method returns `NoSettings` and issues no HTTP request.

### Task 3.2: Add download and verification
`_Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_`

- `DownloadAsync(manifest, IProgress<double>?, CancellationToken)`.
- Reject `ApkUrl` unless `Uri.Scheme == https`, before the request is issued.
- Stream to `FileSystem.CacheDirectory` under a unique filename.
- Compute SHA-256 while streaming and compare case-insensitively to `Sha256`.
- On mismatch or cancellation, delete the file and return failure.
- **Done when:** a doctored `Sha256` produces a failure and leaves no file behind.

### Task 3.3: Add diagnostic recording
`_Requirements: 7.1, 7.2_`

- Write the last check outcome and timestamp to `Preferences` (not `SecureStorage` — this is not a secret and `SecureStorage` is slower).
- Add a small read accessor for the gate to display.
- **Done when:** outcomes persist across restarts and are readable.

### Task 3.4: Register the service
`_Requirements: 1.5_`

- File: `Mobile.MAUI/Services/Extensions.cs:10-29`.
- Add `services.AddSingleton<AppUpdateService>();` next to `ApiClientService` (line 20).
- **Done when:** the service is resolvable from DI.

**Checkpoint 3:** Build `net9.0-windows10.0.19041.0` and exercise `AppUpdateService` against the local API. Everything except the install step is testable here.

---

## Phase 4 — Enforcement UI

### Task 4.1: Create the gate component
`_Requirements: 2.2, 2.3, 2.4, 7.2, 7.3_`

- Files: `Mobile.MAUI/Components/UpdateGate.razor` and `.razor.cs` (new).
- States: `Checking | Ready | Blocked | Downloading | Failed`.
- On first render, await the check. `NoSettings`, `UpToDate`, and `CheckFailed` → `Ready`. `Optional` and `Mandatory` → `Blocked`.
- `Ready` renders `<Routes />`.
- `Blocked` shows installed version (`AppInfo.Current.VersionString`), target version, release notes, an Update button, and a support/escalation line. Dismissible **only** when `Optional`.
- `Downloading` shows progress and a Cancel button.
- `Failed` shows the reason and a Retry button; remains blocked when mandatory.
- **Done when:** with the API returning a `VersionCode` equal to the installed version, the app renders the normal router with no prompt.

### Task 4.2: Wire the gate above the router
`_Requirements: 2.2, P1_`

- File: `Mobile.MAUI/Components/Routes.razor:4-16`.
- Wrap the existing `<Router>` in `<UpdateGate>` rather than replacing it.
- Keep `CascadingAuthenticationState`, `AuthorizeRouteView`, `DefaultLayout`, and `FocusOnNavigate` exactly as they are.
- **Done when:** blocking hides all routes and unblocking restores them unchanged.

### Task 4.3: Guard against mid-session enforcement
`_Requirements: 2.5, 2.6, P4_`

- Ensure the check runs **only** on gate initialization — no timers, no `Connectivity.ConnectivityChanged` subscription.
- **Done when:** navigating between pages never re-triggers a block.

**Checkpoint 4:** Three manual runs — up-to-date device, offline launch, and a mandatory manifest. The first two reach login; the third blocks.

---

## Phase 5 — Android install path

### Task 5.1: Resolve the package-name discrepancy
`_Requirements: 4.1, 4.3_`

- Files: `Mobile.MAUI/Platforms/Android/AndroidManifest.xml:2` vs `Mobile.MAUI/Mobile.MAUI.csproj:28`.
- Investigation task: `package="com.wmsmobile.appname"` conflicts with `ApplicationId` `com.WMS.mobile.maui`. Determine the actual runtime package name and use **that** for the FileProvider authority and the unknown-sources settings intent.
- Do not change either value as part of this feature — record the finding and build against reality.
- **Done when:** the runtime package name is confirmed by logging `AppInfo.Current.PackageName` on a device.

### Task 5.2: Declare permissions and the provider
`_Requirements: 4.1, 6.4_`

- File: `Mobile.MAUI/Platforms/Android/AndroidManifest.xml`.
- Add `<uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />` beside the existing permissions (lines 12-14).
- Add a `<provider>` inside `<application>` (lines 3-11): `android:authorities="<runtime package>.fileprovider"`, `android:exported="false"`, `android:grantUriPermissions="true"`, meta-data `android.support.FILE_PROVIDER_PATHS` → `@xml/file_paths`.
- Leave `usesCleartextTraffic` untouched.
- **Done when:** the app builds and installs with no manifest-merger errors.

### Task 5.3: Create the file paths resource
`_Requirements: 3.1, 4.1_`

- File: `Mobile.MAUI/Platforms/Android/Resources/xml/file_paths.xml` (new; the directory does not exist yet).
- Expose only `<cache-path name="updates" path="." />` — nothing broader.
- **Done when:** the resource resolves and the provider exposes only the cache directory.

### Task 5.4: Implement the installer
`_Requirements: 4.1, 4.2, 4.3, 4.5_`

- Files: `Mobile.MAUI/Platforms/Android/ApkInstaller.cs` (new) plus a partial declaration in the shared `AppUpdateService` so Phase 3 code stays platform-agnostic.
- `PackageManager.CanRequestPackageInstalls()` guarded by `Build.VERSION.SdkInt >= BuildVersionCodes.O` (min SDK is 24, so 24–25 skip the per-app check and rely on the global setting).
- If not permitted, launch `Settings.ActionManageUnknownAppSources` with the package URI and return a distinct result so the UI can explain.
- Otherwise build the `content://` URI via `FileProvider.GetUriForFile` using the authority from Task 5.1, and launch `Intent.ActionView` with `application/vnd.android.package-archive`, `GrantReadUriPermission`, and `NewTask`.
- Never attempt silent installation.
- **Done when:** a real device shows the Android install dialog for a correctly signed APK.

### Task 5.5: Handle installer outcomes
`_Requirements: 4.4_`

- If the user declines the system dialog, return the gate to `Blocked` with the update still pending and a retry path. Do not treat a decline as success.
- **Done when:** declining returns to the blocking screen.

**Checkpoint 5 (device required):**

1. Enable unknown sources when prompted; confirm the settings screen opens.
2. Install a pilot APK; confirm the app restarts on the new `versionCode`.
3. Decline the system dialog; confirm the gate stays blocked.
4. Set `ApkUrl` to `http://...` in a test manifest; confirm the download is refused.
5. Corrupt `Sha256`; confirm installation is never offered and no file remains in cache.

---

## Phase 6 — Release procedure

### Task 6.1: Write the publish runbook
`_Requirements: 5.1, 5.2, 5.3, 5.4, 5.6, 5.7, 2.7_`

- File: `changelogs.md` (existing, `WMS.slnx:12`) or a new `Api.CoreWebAPI/MobileUpdate/README.md`.
- Document the exact loop:
  1. Bump `ApplicationVersion` in `Mobile.MAUI/Mobile.MAUI.csproj:32`.
  2. Build the Release APK with the release keystore.
  3. `(Get-FileHash "<apk>" -Algorithm SHA256).Hash.ToLowerInvariant()`
  4. Copy the APK to the configured APK directory as `{version}.apk`.
  5. Update `manifest.json` with version, versionCode, hash, and notes; set `Mandatory: false`.
  6. Install on one pilot device and confirm.
  7. Flip `Mandatory: true`.
  8. Revert procedure: restore the previous `manifest.json`.
- **Done when:** someone other than the author can execute it without asking questions.

### Task 6.2: Document keystore and versionCode discipline
`_Requirements: 5.3, 5.4_`

- Add an explicit warning: the same keystore for every release; `ApplicationVersion` must strictly increase; losing the keystore requires uninstalling on every device.
- **Done when:** written and referenced from the runbook.

**Checkpoint 6:** Execute the runbook end to end on a pilot device, including the revert step.

---

## Phase 7 — Preservation verification

### Task 7.1: Regression sweep
`_Requirements: P1, P2, P3, P4, P5, P6_`

- No code. Verify by observation:
  - Login and token refresh unchanged.
  - `endpoint-settings` still loads and saves.
  - 401 handling still routes to login.
  - Scanner broadcast still delivers scans.
  - All existing API routes respond as before.
  - Cold start with the update host unreachable is not noticeably slower.
- **Done when:** all six are observed and recorded.

---

## Optional (not MVP)

- `Optional` Unit tests for version comparison and version-token validation, in a new test project.
- `Optional` Retain N previous manifests in the update folder for one-step rollback.
- `Optional` Publish an update-check log endpoint so stale devices are visible.
- `Optional` Device-owner provisioning to enable silent installs later.

---

## Requirement traceability

| Requirement | Tasks |
|---|---|
| 1.1 | 2.1 |
| 1.2 | 1.1, 1.2, 2.2 |
| 1.3 | 2.2 |
| 1.4 | 1.2, 1.3, 1.4, 2.2 |
| 1.5 | 3.1, 3.4 |
| 1.6 | 3.1, 4.1 |
| 2.1 | 3.1 |
| 2.2 | 4.1, 4.2 |
| 2.3 | 4.1 |
| 2.4 | 4.1 |
| 2.5 | 4.3 |
| 2.6 | 3.1, 4.3 |
| 2.7 | 6.1 |
| 3.1 | 3.2, 5.3 |
| 3.2 | 3.2 |
| 3.3 | 3.2 |
| 3.4 | 3.2, 4.1 |
| 3.5 | 3.2 |
| 3.6 | 3.2 |
| 4.1 | 5.1, 5.2, 5.3, 5.4 |
| 4.2 | 5.4 |
| 4.3 | 5.1, 5.4 |
| 4.4 | 5.5 |
| 4.5 | 5.4 |
| 5.1 | 6.1 |
| 5.2 | 6.1 |
| 5.3 | 6.1, 6.2 |
| 5.4 | 6.1, 6.2 |
| 5.5 | 6.1 |
| 5.6 | 6.1 |
| 5.7 | 1.3, 1.4, 6.1 |
| 6.1 | 2.2 |
| 6.2 | 2.3 |
| 6.3 | Design decision 8 (no code) |
| 6.4 | 5.2 |
| 7.1 | 3.3 |
| 7.2 | 3.3, 4.1 |
| 7.3 | 4.1 |
| 8.1 | 5.4 |
| 8.2 | 3.1, 5.4 |
| P1 | 4.2, 7.1 |
| P2 | 3.1, 7.1 |
| P3 | 7.1 |
| P4 | 4.3, 7.1 |
| P5 | 7.1 |
| P6 | 3.1, 7.1 |

---

## Build commands

```
dotnet build Api.CoreWebAPI/Api.CoreWebAPI.csproj
dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-windows10.0.19041.0
dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android
```

No test command is listed because no test project covers this feature (`WMS.slnx:36-38`).
