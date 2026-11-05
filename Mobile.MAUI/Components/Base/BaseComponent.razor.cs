using Microsoft.JSInterop;
using Mobile.MAUI.Services;

namespace Mobile.MAUI.Components.Base;

public partial class BaseComponent
{
    [CascadingParameter]
    public Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject] public JWTAuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public NavigationManager NavManager { get; set; } = default!;
    [Inject] public DialogService Dialog { get; set; } = default!;
    [Inject] public ToastifyService Toast { get; set; } = default!;
    [Inject] public ApiClientService Client { get; set; } = default!;
    [Inject] public BarcodeResolver BarcodeResolver { get; set; } = default!;
    [Inject] public PrinterApiClientService PrinterApiClient { get; set; } = default!;
    [Inject] public ActionFactoryService ActionFactory { get; set; } = default!;
    [Inject] public RoleService RoleService { get; set; } = default!;
    [Inject] public UnsaveChangesHandlerService UnsaveChangesHandler { get; set; } = default!;
    [Inject] protected IJSRuntime Js { get; set; } = default!;

    protected void MarkScansDirty() => UnsaveChangesHandler.MarkAsDirty();

    protected void MarkScansClean() => UnsaveChangesHandler.MarkAsClean();

    private IJSObjectReference? _lastScannedJs;

    // Imports the panel script on first use. Deliberately lazy: it is only
    // needed once something has been scanned, and by then the page has settled.
    private async Task<IJSObjectReference> GetLastScannedJsAsync()
    {
        if (_lastScannedJs is not null) return _lastScannedJs;

        _lastScannedJs = await Js.InvokeAsync<IJSObjectReference>(
            "import",
            "./js/IntersectionObserver.js");

        await _lastScannedJs.InvokeVoidAsync("ObserveLastScannedPanel");

        return _lastScannedJs;
    }

    protected async Task RevealLastScanned()
    {
        try
        {
            var js = await GetLastScannedJsAsync();
            await js.InvokeVoidAsync("RevealLastScanned");
        }
        catch
        {
            // The panel is a convenience - never let it break a scan.
        }
    }

    protected async Task DisposeLastScannedPanel()
    {
        if (_lastScannedJs is null) return;

        try
        {
            await _lastScannedJs.InvokeVoidAsync("UnObserveLastScannedPanel");
            await _lastScannedJs.DisposeAsync();
        }
        catch
        {
            // Circuit already gone.
        }
        finally
        {
            _lastScannedJs = null;
        }
    }

}
