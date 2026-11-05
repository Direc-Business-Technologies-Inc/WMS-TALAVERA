using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Custom;
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
    DateTime? DateRangeStart;
    DateTime? DateRangeEnd;
    PrintStatus SelectedPrintStatus = PrintStatus.ForPrinting;
    PrintStatusView PrintStatusView = default!;
    bool loadCountsAfterGrid;
    DataGridIntent CountBaseIntent = new();

    string ActionGetTripTickets { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllTripTickets);

    Task<int> PrintCountAsync(PrintStatus status) => CountAsync(status);

    async Task<int> CountAsync(PrintStatus status)
    {
        var intent = CloneIntent(CountBaseIntent);
        intent.Skip = 0;
        intent.Take = 1;
        TripTicketPrintStatusFilter.Apply(intent, status);
        var (_, count) = await TripTicketHandler.GetTTDataGridAsync(intent, CurrentUserService.NsSubsidiaryId);
        return count;
    }

    static DataGridIntent CloneIntent(DataGridIntent intent) => new()
    {
        Skip = intent.Skip,
        Take = intent.Take,
        Sorts = [.. intent.Sorts.Select(sort => new AppSortDescriptor { Property = sort.Property, Direction = sort.Direction })],
        Filters = [.. intent.Filters.Select(CloneFilter)]
    };

    static AppFilterDescriptor CloneFilter(AppFilterDescriptor filter) => new()
    {
        Property = filter.Property,
        Value = filter.Value,
        ComparisonOperator = filter.ComparisonOperator,
        LogicalOperator = filter.LogicalOperator,
        FilterValueType = filter.FilterValueType,
        Filters = [.. filter.Filters.Select(CloneFilter)]
    };

    async Task PrintStatusChanged(PrintStatus status)
    {
        SelectedPrintStatus = status;
        await ReloadFirstPageAsync();
    }

    async Task ReloadFirstPageAsync()
    {
        if (TTDataGrid.DataGrid.CurrentPage == 0)
            await TTDataGrid.DataGrid.Reload();
        else
            await TTDataGrid.DataGrid.GoToPage(0);
    }

    async Task RefreshPrintStatusAsync()
    {
        await ReloadFirstPageAsync();
        await PrintStatusView.RefreshCountsAsync();
    }

    async Task DateRangeChanged((DateTime Start, DateTime End) range)
    {
        DateRangeStart = range.Start;
        DateRangeEnd = range.End;
        await RefreshPrintStatusAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
        if (loadCountsAfterGrid && PrintStatusView is not null)
        {
            loadCountsAfterGrid = false;
            _ = InvokeAsync(PrintStatusView.RefreshCountsAsync);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(TTDataGrid.DataGrid, settings => TTDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await TTDataGrid.DataGrid.ReloadSettings();
        await TTDataGrid.DataGrid.Reload();
        loadCountsAfterGrid = true;
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

                intent.Sorts.Add(new()
                {
                    Property = nameof(TripTicketDataGridVM.Name),
                    Direction = SortDirectionEnum.Descending
                });
            }

            PrintStatusFilter.ApplyDateRange(intent, DateRangeStart, DateRangeEnd, nameof(TripTicketDataGridVM.TripDate));
            CountBaseIntent = CloneIntent(intent);
            TripTicketPrintStatusFilter.Apply(intent, SelectedPrintStatus);
            var result = await TripTicketHandler.GetTTDataGridAsync(intent, CurrentUserService.NsSubsidiaryId);
            if (!DateRangeStart.HasValue && PrintStatusFilter.TryGetDateRange(result.Data, row => row.TripDate, out var range))
            {
                DateRangeStart = range.Start;
                DateRangeEnd = range.End;
                await InvokeAsync(StateHasChanged);
                _ = InvokeAsync(async () =>
                {
                    await Task.Yield();
                    await TTDataGrid.DataGrid.Reload();
                    await PrintStatusView.RefreshCountsAsync();
                });
            }
            return result;

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
