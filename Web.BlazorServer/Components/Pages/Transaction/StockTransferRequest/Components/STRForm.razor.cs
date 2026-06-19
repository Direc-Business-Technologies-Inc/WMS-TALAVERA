using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
using Shared.Entities;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Implementations.Others;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest.Components;

public partial class STRForm
{
    [Parameter]
    [EditorRequired]
    public StockTransferRequestInfoVM Model { get; set; } = new();
    [Parameter]
    public Func<StockTransferRequestInfoVM, Task<bool>>? OnSubmit { get; set; }
    [Parameter]
    public EditContext? EditContext { get; set; }
    [Parameter]
    public bool ReadOnly { get; set; } = false;
    [Parameter]
    public string? ReturnURI { get; set; }
    [Parameter]
    public string? ActionURI { get; set; }
    [Parameter]
    public string ActionLabel { get; set; } = "Submit";
    [Parameter]
    public string ReturnLabel { get; set; } = "Return";

    [Inject]
    IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject]
    ILocationHandler LocationHandler { get; set; } = default!;
    [Inject]
    ISubsidiaryHandler SubsidiaryHandler { get; set; } = default!;
    [Inject]
    IVendorHandler VendorHandler { get; set; } = default!;

    AppTable<StockTransferRequestLineVM> LinesTable = default!;
    DataGridSettings TableSettings { get; set; } = new();

    readonly string ActionGetLocations = "Get Locations";
    readonly string ActionGetSubsidiaries = "Get Subsidiaries";
    readonly string ActionGetVendors = "Get Vendors";

    private List<VendorVM> Vendors { get; set; } = [];
    private List<LocationVM> Locations { get; set; } = [];
    private List<SubsidiaryVM> Subsidiaries { get; set; } = [];

    private int VendorsCount { get; set; } = 1;
    private int LocationsCount { get; set; } = 1; // set counts to one to automatically trigger LoadData
    private int SubsidiariesCount { get; set; } = 1;

    private bool IsLoadingLocations => AppBusyService.IsBusy(ActionGetLocations);
    private bool IsLoadingSubsidiaries => AppBusyService.IsBusy(ActionGetSubsidiaries);
    private bool IsLoadingVendors => AppBusyService.IsBusy(ActionGetVendors);
    private List<TransferCategory> ReturnCategories = [.. TransferCategory.ReturnCategories];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await Task.WhenAll(
                LoadGridSettings(),
                LoadVendors(new()),
                LoadSubsidiaries(new()));
        }
    }

    async Task LoadGridSettings()
    {
        if (GridSettingsLoaded) return;

        await GridSettingsService.SetGridSettings(LinesTable.DataGrid, settings => TableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await LinesTable.DataGrid.ReloadSettings();
        await LinesTable.DataGrid.Reload();
    }

    async Task HandleSubmit()
    {
        if (Model.Lines.Count == 0)
        {
            await DialogService.Alert("Please add at least one item", "Error");
            return;
        }

        bool success = true;
        if (OnSubmit is not null) success = await OnSubmit(Model);
        if (success && !string.IsNullOrEmpty(ActionURI))
        {
            NavManager.NavigateTo(ActionURI, true);
        }
    }

    async Task LoadLocations(LoadDataArgs args, int? subsidiaryId)
    {
        if (subsidiaryId is null) return;

        var action = await AppActionFactory.RunAsync(async () =>
        {

            AppBusyService.SetBusy(ActionGetLocations, true);
            var DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(LocationVM.Name),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });

            var response = await LocationHandler.GetLocationsBySubsidiaryAsync(DatagridAdapter.QueryIntent, (int)subsidiaryId);

            Locations = [.. response.Data];
            LocationsCount = response.Count;
            AppBusyService.SetBusy(ActionGetLocations, false);
            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetLocations));

    }

    async Task LoadSubsidiaries(LoadDataArgs args)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetSubsidiaries, true);
            var DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(SubsidiaryVM.Name),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });

            var response = await SubsidiaryHandler.GetSubsidiariesAsync(DatagridAdapter.QueryIntent);

            Subsidiaries = [.. response.Data];
            SubsidiariesCount = response.Count;

            AppBusyService.SetBusy(ActionGetSubsidiaries, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetSubsidiaries));
    }

    async Task LoadVendors(LoadDataArgs args)
    {
        if (Model.ToSubsidiary is null) return;
        var sudsidiaryId = Model.ToSubsidiary.Id;
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetVendors, true);
            var DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(VendorVM.Name),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });


            var response = await VendorHandler.GetVendorsListBySubsidiaryAsync(DatagridAdapter.QueryIntent, sudsidiaryId);

            Vendors = [.. response.Data];
            VendorsCount = response.Count;

            AppBusyService.SetBusy(ActionGetVendors, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetVendors));
    }

    async Task AddItems(List<ItemsVM> items)
    {
        foreach (var item in items)
        {
            Model.Lines.Add(new()
            {
                ItemId = item.Id,
                ItemCode = item.ItemNumber,
                ItemDescription = item.Name,
                Warehouse = Model.SourceLocation?.Name ?? string.Empty,
                UoM = item.StockUnit,
                QuantityOnHand = item.QuantityOnHand,
                QuantityAlloted = 0
            });
        }
        await LinesTable.DataGrid.Reload();
        await InvokeAsync(StateHasChanged);
    }

    async Task OnSubsidiaryChanged(SubsidiaryVM? value)
    {

        if (Model.Lines.Any())
        {
            var confirm = await DialogService.Confirm(message: "Changing subsidiaries will clear added items") ?? false;
            if (!confirm) return;
        }
        if (value != Model.Subsidiary)
        {
            Model.Lines.Clear();
            Model.Subsidiary = value;
            Model.SourceLocation = null;
            if (!Model.IsIntercompany)
            {
                Model.ToSubsidiary = value;
                Model.DestinationLocation = null;
            }
            await InvokeAsync(StateHasChanged);
        }
    }


    async Task OnLocationChanged(LocationVM? value)
    {
        if (Model.Lines.Any())
        {
            var confirm = await DialogService.Confirm(message: "Changing source warehouse will clear added items") ?? false;
            if (confirm)
            {
                Model.SourceLocation = value;
                Model.Lines.Clear();
            }
        }
        else
        {
            Model.SourceLocation = value;
        }
        await InvokeAsync(StateHasChanged);
    }

    async Task OnToSubsidiaryChanged(SubsidiaryVM? value)
    {
        LocationsCount = 1;
        Model.ToSubsidiary = value;
        Model.DestinationLocation = null;
        Model.Vendor = null;
    }

    async Task DeleteLine(StockTransferRequestLineVM line)
    {
        Model.Lines.Remove(line);
        await LinesTable.DataGrid.Reload();
    }

    void Return()
    {
        if (!string.IsNullOrEmpty(ReturnURI)) NavManager.NavigateTo(ReturnURI, true);
    }

    void ActionClicked()
    {
        if (ReadOnly && !string.IsNullOrEmpty(ActionURI)) NavManager.NavigateTo(ActionURI, true);
    }
}
