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
    bool IsReturn => Model.Type == StockTransferRequestInfoVM.Types.Returns;

    readonly string ActionGetLocations = "Get Locations";
    readonly string ActionGetSubsidiaries = "Get Subsidiaries";
    readonly string ActionGetVendors = "Get Vendors";

    private List<VendorVM> Vendors { get; set; } = [];
    private List<LocationVM> Locations { get; set; } = [];
    private List<SubsidiaryVM> Subsidiaries { get; set; } = [];

    private int VendorsCount { get; set; } = 0;
    private int LocationsCount { get; set; } = 0;
    private int SubsidiariesCount { get; set; } = 0;

    private bool IsLoadingLocations => AppBusyService.IsBusy(ActionGetLocations);
    private bool IsLoadingSubsidiaries => AppBusyService.IsBusy(ActionGetSubsidiaries);
    private bool IsLoadingVendors => AppBusyService.IsBusy(ActionGetVendors);
    public StockTransferRequestInfoVM _Model { get; set; } = new();



    protected override void OnParametersSet()
    {
        Model.Adapt(_Model);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await Task.WhenAll(
                LoadGridSettings(),
                LoadLocations(new()),
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
        bool success = true;
        if (OnSubmit is not null) success = await OnSubmit(Model);
        if (success && !string.IsNullOrEmpty(ActionURI))
        {
            NavManager.NavigateTo(ActionURI, true);
        }
    }

    async Task LoadLocations(LoadDataArgs args)
    {
        if (IsLoadingLocations) return;
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

            var response = await LocationHandler.GetLocationsAsync(DatagridAdapter.QueryIntent);

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

            var response = await VendorHandler.GetVendorsListAsync(DatagridAdapter.QueryIntent);

            Vendors = [.. response.Data];
            VendorsCount = response.Count;

            AppBusyService.SetBusy(ActionGetVendors, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetVendors));
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
