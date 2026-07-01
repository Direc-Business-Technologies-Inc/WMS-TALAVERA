using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket.Component;

public partial class TripTicketFulfillmentSelection
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    [Parameter] public required TripTicketVM Document { get; set; }
    [Parameter] public EventCallback<TripTicketVM> DocumentChanged { get; set; }
    [Parameter] public IEnumerable<ItemFulfillmentVM> Fulfillments { get; set; } = [];

    AppDataGrid<ItemFulfillmentVM> FulfillmentsDataGrid { get; set; } = default!;
    DataGridSettings FulfillmentsDataGridSettings { get; set; } = new();

    string ActionGetFulfillments { get; } = EnumHelper.GetEnumDescription(AppActions.GetPackedTripTicketFulfillments);

    IList<ItemFulfillmentVM> SelectedFulfillments { get; set; } = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadGridSettings();
        }
    }

    async Task LoadGridSettings()
    {
        SelectedFulfillments = [.. Document.ItemFulfillments];

        await GridSettingsService.SetGridSettings(FulfillmentsDataGrid.DataGrid, settings => FulfillmentsDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;
        await FulfillmentsDataGrid.DataGrid.ReloadSettings();
        await FulfillmentsDataGrid.DataGrid.Reload();
    }

    Task<DataGridResultVM<ItemFulfillmentVM>> LoadDataAsync(DataGridIntent intent)
    {
        List<ItemFulfillmentVM> data = [.. Fulfillments];
        return Task.FromResult(DataGridResultVM<ItemFulfillmentVM>.New(data, data.Count));
    }

    async Task OnRowSelect(ItemFulfillmentVM data)
    {
        if (!SelectedFulfillments.Any(x => x.NetsuiteOrderInternalId == data.NetsuiteOrderInternalId))
            SelectedFulfillments.Add(data);

        if (!Document.ItemFulfillments.Any(x => x.NetsuiteOrderInternalId == data.NetsuiteOrderInternalId))
            Document.ItemFulfillments = [.. Document.ItemFulfillments, data];

        await DocumentChanged.InvokeAsync(Document);
        await InvokeAsync(StateHasChanged);
    }

    async Task OnRowDeselect(ItemFulfillmentVM data)
    {
        var existing = SelectedFulfillments.FirstOrDefault(x => x.NetsuiteOrderInternalId == data.NetsuiteOrderInternalId);
        if (existing is not null)
            SelectedFulfillments.Remove(existing);

        if (Document.ItemFulfillments.Any(x => x.NetsuiteOrderInternalId == data.NetsuiteOrderInternalId))
            Document.ItemFulfillments = [.. Document.ItemFulfillments.Where(x => x.NetsuiteOrderInternalId != data.NetsuiteOrderInternalId)];

        await DocumentChanged.InvokeAsync(Document);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await GridSettingsService.UnsetGridSettings(FulfillmentsDataGrid.DataGrid);
    }
}
