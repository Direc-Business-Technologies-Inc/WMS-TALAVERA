using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Pages.Transaction.SupplierReturn;

public partial class SupplierReturnUpdate
{
    [Inject] ISupplierReturnHandler returnHandler { get; set; } = default!;
    [Inject] AppAuthenticationService authService { get; set; } = default!;
    [Inject] IBusyDialogService busyDialogService { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Ref { get; set; } = null;

    bool IsBusy = false;

    bool IsLoadingData => AppBusyService.IsBusy(ActionGetReturn);
    readonly string ActionGetReturn = "Get Return to Supplier";
    readonly string ActionUpdateReturn = "Update Return to Supplier";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGetReturn, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }

    async Task LoadDataAsync()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetReturn, true);

            if (Ref is null) throw new Exception("Please select a Return to Supplier");


            return await returnHandler.GetReturnAsync(Ref);

        }, ActionGetReturn);

        action.OnFailure(async (ex) =>
        {
            await Task.Delay(120);
            NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
        });

        action.OnSuccess(async (response) =>
        {
            response.Adapt(FormData);
            PrepareFormData();
            await InvokeAsync(StateHasChanged);
        });
    }

    void PrepareFormData()
    {
        var nameClaim = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
        FormData.Memo = FormData.Memo + $"\nUpdated via WMS on {DateTime.Now.ToString("MMMM dd, yyyy hh:mmtt")}";
        FormData.PreparedBy = string.IsNullOrEmpty(nameClaim) ? "No Netsuite Account Registered" : nameClaim;
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }
    async Task Return(SupplierReturnVM _)
    {
        NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
    }

    async Task Submit(SupplierReturnVM data)
    {
        IsBusy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            var action = await AppActionFactory.RunConfirmedAsync(async () =>
            {
                busyDialogService.Show(ActionUpdateReturn);

                try
                {
                    await returnHandler.UpdateSupplierReturnAsync(data);
                }
                finally
                {
                    busyDialogService.Hide();
                }

            }, ActionUpdateReturn);

            action.OnSuccess(async () =>
            {
                await Task.Delay(100);
                NavManager.NavigateTo(SupplierReturnRoutes.INDEX);
            });
        }
        catch (Exception ex)
        {
            ToastService.Error(
                "An error occurred while updating the supplier return."
            );
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}