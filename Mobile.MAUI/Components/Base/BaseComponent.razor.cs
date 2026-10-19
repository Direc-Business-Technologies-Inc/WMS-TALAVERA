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
    [Inject] public PrinterApiClientService PrinterApiClient { get; set; } = default!;
    [Inject] public ActionFactoryService ActionFactory { get; set; } = default!;
    [Inject] public RoleService RoleService { get; set; } = default!;
    [Inject] public UnsaveChangesHandlerService UnsaveChangesHandler { get; set; } = default!;
    [Inject] protected IJSRuntime Js { get; set; } = default!;

    protected void MarkScansDirty() => UnsaveChangesHandler.MarkAsDirty();

    protected void MarkScansClean() => UnsaveChangesHandler.MarkAsClean();

    private IJSObjectReference? _lastScannedJs;
    private bool _lastScannedRevealPending;

    protected async Task InitLastScannedPanel(IJSObjectReference js)
    {
        _lastScannedJs = js;

        await js.InvokeVoidAsync("ObserveLastScannedScroll");

        // A scan can land before the module is imported - the import is gated on
        // the item grid having rows, which happens on first render. Replay it.
        if (_lastScannedRevealPending)
        {
            _lastScannedRevealPending = false;
            await js.InvokeVoidAsync("ShowLastScanned");
        }
    }

    protected async Task RevealLastScanned()
    {
        if (_lastScannedJs is null)
        {
            _lastScannedRevealPending = true;
            return;
        }

        await _lastScannedJs.InvokeVoidAsync("ShowLastScanned");
    }

    protected void DisposeLastScannedPanel()
    {
        _lastScannedJs?.InvokeVoidAsync("UnObserveLastScannedScroll");
        _lastScannedJs = null;
        _lastScannedRevealPending = false;
    }

}
