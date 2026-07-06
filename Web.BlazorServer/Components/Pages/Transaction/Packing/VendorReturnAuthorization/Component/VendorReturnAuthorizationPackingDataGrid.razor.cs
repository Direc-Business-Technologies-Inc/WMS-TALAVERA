using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Pages.Transaction.Packing;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

namespace Web.BlazorServer.Components.Pages.Transaction.Packing.VendorReturnAuthorization.Component;

partial class VendorReturnAuthorizationPackingDataGrid
{
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IVendorReturnAuthorizationPackingHandler VraHandler { get; set; } = default!;

    AppDataGrid<VendorReturnAuthorizationPackingDataGridVM> DataGrid { get; set; } = default!;
    DataGridSettings DataGridSettings { get; set; } = new();

    string ActionGetVendorReturnAuthorizations { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllPackingVendorReturnAuthorizations);

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
        await GridSettingsService.SetGridSettings(DataGrid.DataGrid, settings => DataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await DataGrid.DataGrid.ReloadSettings();
        await DataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<VendorReturnAuthorizationPackingDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetVendorReturnAuthorizations, true);

            if (intent.Sorts.Count == 0)
            {
                intent.Sorts.Add(new()
                {
                    Property = nameof(VendorReturnAuthorizationPackingDataGridVM.Date),
                    Direction = SortDirectionEnum.Descending
                });
            }

            return await VraHandler.GetVendorReturnAuthorizationsList(intent, CurrentUserService.NsSubsidiaryId);
        }, AppActionOptionPresets.Loading(ActionGetVendorReturnAuthorizations));

        AppBusyService.SetBusy(ActionGetVendorReturnAuthorizations, false);
        return DataGridResultVM<VendorReturnAuthorizationPackingDataGridVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void ViewVendorReturnAuthorization(VendorReturnAuthorizationPackingDataGridVM item)
    {
        NavManager.NavigateTo(PackingRoutes.VendorReturnAuthorizationView + $"?ref={item.ReferenceNumber}", true);
    }
}
