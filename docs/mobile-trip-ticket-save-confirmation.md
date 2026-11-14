# Mobile Trip Ticket Save Confirmation

## Behavior

- After the API reports a successful save, mobile displays **“Trip ticket saved successfully.”** for **8 seconds**, then the toast expires normally. Navigation home proceeds immediately after the toast is created.
- If Toastify reports a rendering failure or JavaScript interop fails, an acknowledgment dialog displays the same confirmation. Mobile waits for dismissal before navigating home.
- The page owns save feedback, preventing the action factory from displaying a second generic success toast. Returned API failures still display an error through `OnFailure`.
- Ordinary scan/success notifications retain their **3-second** duration. `ToastifyService.Success(message, durationMs)` returns whether Toastify created the notification; existing callers may ignore the result.

## Verification

Run `node --test Mobile.MAUI.Tests/toastifyPresets.test.mjs` to check default/custom durations and reported rendering failures.

On a mobile device, confirm one success toast remains visible after navigation, simulate a missing Toastify function to check the acknowledgment fallback, and verify API failures display an error without success navigation.

This improves confirmation visibility after a successful API response. It does not validate the RESTlet's business-result body or resolve saves whose responses never reach mobile.
