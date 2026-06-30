using Microsoft.AspNetCore.Components;
using Shared.Kernel;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.ViewModels.System;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket;

partial class TripTicketCVU
{
    [Inject] ITripTicketHandler TripTicketHandler { get; set; } = default!;

    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }

    bool Creating { get; set; }
    bool Viewing => !Creating;
    bool IsLoadingData => AppBusyService.IsBusy(ActionViewTripTicket);

    TripTicketDataGridVM? TripTicket { get; set; }

    readonly string ActionViewTripTicket = EnumHelper.GetEnumDescription(AppActions.ViewTripTicket);

    List<NavigationRouteVM> AdditionalRoutes { get; set; } =
    [
        new()
        {
            Name = "Trip Ticket",
            Position = 0,
            Icon = "transit_ticket",
            Uri = TripTicketRoutes.Root
        }
    ];

    protected override void OnParametersSet()
    {
        var relativePath = NavManager.ToBaseRelativePath(NavManager.Uri);
        Creating = relativePath.StartsWith(TripTicketRoutes.Create.TrimStart('/'), StringComparison.OrdinalIgnoreCase);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender && Viewing)
            await LoadDataAsync();
    }

    async Task LoadDataAsync()
    {
        if (Ref <= 0)
        {
            NavError("Please select a trip ticket from the list");
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionViewTripTicket, true);
            return await TripTicketHandler.GetTripTicketAsync(Ref);
        }, AppActionOptionPresets.Loading(ActionViewTripTicket));

        AppBusyService.SetBusy(ActionViewTripTicket, false);

        action.OnSuccess(async args =>
        {
            if (action.Result is null)
            {
                NavError($"Trip Ticket \"{Ref}\" could not be found");
                return;
            }

            TripTicket = action.Result;
            await InvokeAsync(StateHasChanged);
        });

        action.OnFailure(ex =>
        {
            NavError(ex.Message);
            return Task.CompletedTask;
        });
    }

    void Back()
    {
        NavManager.NavigateTo(TripTicketRoutes.Root, true);
    }

    void NavError(string message)
    {
        ToastService.Error(message);
        NavManager.NavigateTo(TripTicketRoutes.Root, true);
    }
}
