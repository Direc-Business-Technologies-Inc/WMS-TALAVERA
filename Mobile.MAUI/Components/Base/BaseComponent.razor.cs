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
    [Inject] protected IJSRuntime Js { get; set; } = default!;

}
