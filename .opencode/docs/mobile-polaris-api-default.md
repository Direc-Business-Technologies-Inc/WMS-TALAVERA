# Mobile Polaris API Default

## Summary

The mobile MAUI app now selects the Polaris API endpoint when it starts and no API endpoint has been saved on the device:

```text
http://192.168.5.27:8768/api
```

The endpoint is represented by protocol `http`, server `192.168.5.27`, port `8768`, and base endpoint `/api`.

## Implementation

- `Mobile.MAUI/Helpers/EndpointDefaults.cs` defines the Polaris endpoint in one place.
- `Mobile.MAUI/Components/Layout/CleanLayout/CleanLayout.razor` initializes the API client for the login layout.
- If `endpoint-settings` is absent or blank in MAUI `SecureStorage`, the layout selects Polaris and saves that endpoint using the existing storage key and `ClientEndpointVM` format.
- If endpoint settings already exist, they are loaded and kept. This preserves a device's configured endpoint during an APK update.
- The existing endpoint settings dialog remains available for changing the endpoint. Saving a new endpoint continues to update the API client immediately.

## Operational Notes

- The default is only used when a device has no saved endpoint settings. It does not forcibly migrate devices with an existing saved endpoint.
- Devices must be able to reach `192.168.5.27` on the local network and TCP port `8768`.
- `localhost` is not used: on Android, it refers to the handset itself, not the server at the branch.
- The configured endpoint uses HTTP, matching the confirmed Polaris URL. HTTP does not encrypt traffic; the server/network configuration should account for that.

## Verification

Build the Android target from the repository root:

```powershell
dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android --no-restore
```

For device verification, clear app data or use a fresh install, launch the app, and confirm login/API calls reach `http://192.168.5.27:8768/api`. Also verify an existing configured endpoint is retained after updating the APK.
