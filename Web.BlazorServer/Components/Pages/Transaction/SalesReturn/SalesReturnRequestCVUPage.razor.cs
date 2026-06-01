using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SalesReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.SalesReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SalesReturn;

public partial class SalesReturnRequestCVUPage
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter]
    public int Ref { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] ISalesReturnHandler SalesReturnHandler { get; set; } = default!;
    [Inject] ISchoolYearHandler SchoolYearHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionGetSalesReturnRequest) || AppBusyService.IsBusy(ActionCreateSalesReturn);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetSalesReturnRequest);

    readonly string ActionGetSalesReturnRequest = EnumHelper.GetEnumDescription(AppActions.ViewSalesReturnRequest);
    readonly string ActionCreateSalesReturn = EnumHelper.GetEnumDescription(AppActions.CreateSalesReturn);
    readonly string ActionGetSalesReturnTypes = EnumHelper.GetEnumDescription(AppActions.GetSalesReturnTypes);
    readonly string ActionGetSchoolYears = EnumHelper.GetEnumDescription(AppActions.GetSchoolYears);

    int SchoolYearsCount { get; set; } = 0;
    #endregion Primitives

    #region Data Structures
    AppTable<SalesReturnRequestLineVM> SalesReturnRequestTable { get; set; } = default!;
    DataGridSettings SalesReturnRequestTableSettings { get; set; } = new();
    List<ReturnTypeVM> ReturnTypes { get; set; } = [];
    List<SchoolYearVM> SchoolYears { get; set; } = [];

    public IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    #endregion Data Structures

    #region Overrides
    protected override void OnParametersSet()
    {
        PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetSalesReturnRequest, true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override async Task CancelEditing()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Sales Return Creation"))
                return;

        NavManager.NavigateTo($"/transactions/sales/sales-return/request/view?Ref={FormData.SapReference.DocEntry}", true);
    }

    protected override async Task HandleSubmit()
    {
        if (FormData.DocumentLines.Count <= 0)
        {
            ToastService.Warning("Please select Items to Return");
            return;
        }

        if (FormData.DocumentLines.All(x => x.Quantity <= 0))
        {
            ToastService.Warning("All Items in the Document have no Quantity.");
            return;
        }

        if (FormData.DocumentLines.Any(x => x.Quantity <= 0))
        {
            if (!await AlertService.PromptAsync("Some Items in the Sales Return have no Quantity. These Items will be removed from the transaction. Are you sure you want to proceed?"))
                return;
            FormData.DocumentLines.RemoveAll(x => x.Quantity <= 0);
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateSalesReturn, true);

            bool response = await SalesReturnHandler.PostSalesReturnFromRequestAsync(FormData);

            return response;
        }, AppActionOptionPresets.Confirmed(ActionCreateSalesReturn));

        AppBusyService.SetBusy(ActionCreateSalesReturn, false);

        action.OnSuccess(async (_) =>
        {
            NavManager.NavigateTo("/transactions/sales/sales-return?T=srr", true);
        });
    }

    protected override async Task InitializeEditing()
    {
        NavManager.NavigateTo($"/transactions/sales/sales-return/request/create?Ref={FormData.SapReference.DocEntry}", true);
    }
    #endregion Overrides

    #region Custom Functions

    async Task LoadDataAsync()
    {
        if (!AppBusyService.IsBusy(ActionGetSalesReturnRequest))
        {
            AppBusyService.SetBusy(ActionGetSalesReturnRequest, true);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
        }

        await Task.WhenAll(
            GetSalesReturnRequest(),
            LoadReturnTypes(),
            LoadSchoolYears(new()));

        FormData.PreparedBy = AuthenticationService.GetUserName();

        AppBusyService.SetBusy(ActionGetSalesReturnRequest, false);
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        if (!GridSettingsLoaded && !IsLoadingData)
            await LoadGridSettings();
    }

    async Task GetSalesReturnRequest()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            var result = await SalesReturnHandler.GetSalesReturnRequestAsync(Ref);
            AppBusyService.SetBusy(ActionGetSalesReturnRequest, false);
            return result;
        }, AppActionOptionPresets.Loading(ActionGetSalesReturnRequest));

        action.OnSuccess(async (args) =>
        {
            if (action.Result is null)
                ToastService.Error("Sales Return Request not found");
            else
            {
                action.Result.Adapt(FormData);
                FormData.PreparedBy = AuthenticationService.GetUserName();
            }
        });
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(SalesReturnRequestTable.DataGrid, settings => SalesReturnRequestTableSettings = settings ?? new());
        GridSettingsLoaded = true;

        await SalesReturnRequestTable.DataGrid.ReloadSettings();
        await SalesReturnRequestTable.DataGrid.Reload();
    }

    async Task Back()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Sales Return Creation"))
                return;

        NavManager.NavigateTo("/transactions/sales/sales-return?T=srr", true);
    }

    async Task LoadSchoolYears(LoadDataArgs args)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            await Task.Yield();

            AppBusyService.SetBusy(ActionGetSchoolYears, true);

            DatagridAdapter = new DataGridIntentAdapter(args);
            DatagridAdapter.AdaptToPagination();
            if (DatagridAdapter.QueryIntent.Take <= 0)
                DatagridAdapter.QueryIntent.Take = 5;

            if (!string.IsNullOrEmpty(args.Filter))
                DatagridAdapter.QueryIntent.Filters.Add(new()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    Property = nameof(SchoolYearVM.Code),
                    Value = args.Filter,
                    ComparisonOperator = ComparisonOperatorEnum.Contains
                });

            (IEnumerable<SchoolYearVM> Data, int Count) = await SchoolYearHandler.GetSchoolYearsAsync(DatagridAdapter.QueryIntent);

            SchoolYears = [.. Data];
            SchoolYearsCount = Count;

            AppBusyService.SetBusy(ActionGetSchoolYears, false);

            await InvokeAsync(StateHasChanged);
        }, AppActionOptionPresets.Loading(ActionGetSchoolYears));
    }

    async Task LoadReturnTypes()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetSalesReturnTypes, true);

            ReturnTypes = [.. await SalesReturnHandler.GetReturnTypesAsync()];

            AppBusyService.SetBusy(ActionGetSalesReturnTypes, false);
        }, AppActionOptionPresets.Loading(ActionGetSalesReturnTypes));
    }

    #endregion Custom Functions
}
