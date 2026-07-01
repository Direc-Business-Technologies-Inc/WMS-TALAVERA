using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket.Component;

partial class TripTicketDataGrid
{
    [Inject] ITripTicketHandler TripTicketHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<TripTicketDataGridVM> TTDataGrid { get; set; } = default!;
    DataGridSettings TTDataGridSettings { get; set; } = new();

    string ActionGetTripTickets { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllTripTickets);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(TTDataGrid.DataGrid, settings => TTDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await TTDataGrid.DataGrid.ReloadSettings();
        await TTDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<TripTicketDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetTripTickets, true);

            if (intent.Sorts.Count == 0)
            {
                intent.Sorts.Add(new()
                {
                    Property = nameof(TripTicketDataGridVM.TripDate),
                    Direction = SortDirectionEnum.Descending
                });
            }

            return await TripTicketHandler.GetTTDataGridAsync(intent);

        }, AppActionOptionPresets.Loading(ActionGetTripTickets));

        AppBusyService.SetBusy(ActionGetTripTickets, false);
        return DataGridResultVM<TripTicketDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewTT(TripTicketDataGridVM tripTicket)
    {
        NavManager.NavigateTo($"{TripTicketRoutes.View}?ref={tripTicket.NetsuiteTripTicketInternalId}", true);
    }

    void CreateTT()
    {
        NavManager.NavigateTo(TripTicketRoutes.Create, true);
    }
}
