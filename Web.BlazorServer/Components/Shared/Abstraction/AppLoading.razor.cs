using Microsoft.AspNetCore.Components;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppLoading
{
    #region Parameters
    [Parameter] required public bool Loading { get; set; }
    [Parameter] public EventCallback<bool> LoadingChanged { get; set; }
    #endregion Parameters
}
