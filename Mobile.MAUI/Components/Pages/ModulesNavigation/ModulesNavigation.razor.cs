using Mobile.MAUI.Helpers.Extensions;
using static Mobile.MAUI.Enums.CustomEnum;

namespace Mobile.MAUI.Components.Pages.ModulesNavigation;

public partial class ModulesNavigation
{
    protected override async Task OnInitializedAsync()
    {
        var authst = await AuthStateProvider.GetAuthenticationStateAsync();
        if (!await AuthState.Authenticated())
        {
            NavManager.NavigateTo("/login", forceLoad: false, replace: true);
        }
    }


    async Task ConfirmModule(ModuleNavigation module)
    {
        switch (module)
        {
            case ModuleNavigation.Receiving:
                NavManager.NavigateTo("/receiving", false, true);
                break;
            case ModuleNavigation.Packing:
                NavManager.NavigateTo("/packing", false, true);
                break;
            case ModuleNavigation.TripTicket:
                NavManager.NavigateTo("/tripticket/create", false, true);
                break;
            case ModuleNavigation.InventoryCounting:
                NavManager.NavigateTo("/inventorycounting", false, true);
                break;
            case ModuleNavigation.InventoryWorksheet:
                NavManager.NavigateTo("/inventorycounting/worksheet", false, true);
                break;
            default:
                NavManager.NavigateTo("/", false, true);
                break;
        }
    }
}
