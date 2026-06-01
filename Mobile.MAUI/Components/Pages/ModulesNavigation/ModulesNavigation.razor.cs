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
            NavManager.NavigateTo("/login", forceLoad: true, replace: true);
        }
    }


    async Task ConfirmModule(ModuleNavigation module)
    {

        switch (module)
        {
            case ModuleNavigation.Receiving:
                NavManager.NavigateTo("/receiving", true, true);
                break;
        }
    }
}
