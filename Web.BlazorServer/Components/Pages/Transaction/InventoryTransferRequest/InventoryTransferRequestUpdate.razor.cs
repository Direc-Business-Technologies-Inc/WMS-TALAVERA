using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest;

public partial class InventoryTransferRequestUpdate
{

    [Inject] AppAuthenticationService authService { get; set; } = default!;
    [Inject] IInventoryTransferRequestHandler itrHandler { get; set; } = default!;
    [Inject] IBusyDialogService busyDialogService { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Ref { get; set; } = null;

    bool IsLoadingData => AppBusyService.IsBusy(ActionGet);
    readonly string ActionGet = "Get Inventory transfer request";
    readonly string ActionUpdate = "Update Inventory transfer request";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGet, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }

    async Task LoadDataAsync()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            if (Ref is null) throw new Exception("Please select an inventory transfer request");

            return await itrHandler.GetInventoryTransferRequestAsync(Ref);
        }, ActionGet);

        action.OnSuccess(async (res) =>
        {
            res.Adapt(FormData);
            PrepareFormData();
            await InvokeAsync(StateHasChanged);
        });

        action.OnFailure(ex =>
        {
            return Task.Delay(100).ContinueWith(_ =>
            {
                NavManager.NavigateTo(ITRRoutes.INDEX);
            });
        });
    }

    void PrepareFormData()
    {
        var destLines = FormData.Lines.Where(x => x.Location?.Id != FormData.SourceLocation?.Id);
        FormData.Lines = [.. destLines];
        FormData.DestinationLocation = destLines.FirstOrDefault(x => x.Location is not null)?.Location;


        var nameClaim = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
        FormData.Memo = FormData.Memo + $"\nUpdated via WMS on {DateTime.Now.ToString("MMMM dd, yyyy hh:mmtt")}";
        FormData.PreparedBy = string.IsNullOrEmpty(nameClaim) ? "No Netsuite Account Registered" : nameClaim;
    }


    async Task OnSubmit(InventoryTransferRequestVM data)
    {

        if (data.Lines.Count == 0)
        {
            ToastService.Error("Please add at least one line to the inventory transfer request.");
            return;
        }
        if (LinesNeedAssignment(data, out var lines))
        {
            ToastService.Error("Please assign inventory details to lines: " + string.Join(", ", lines.Select(l => l.ItemCode)));
            return;
        }

        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            busyDialogService.Show(ActionUpdate);

            foreach (var item in data.Lines.Where(x => x.LineNumber is not null))
            {
                item.InventoryDetails.Clear();    
            }
            await itrHandler.UpdateInventoryTransferRequest(data);
        }, ActionUpdate);

        busyDialogService.Hide();
        action.OnSuccess(() =>
        {
            NavManager.NavigateTo(ITRRoutes.INDEX);
            return Task.CompletedTask;
        });


        await InvokeAsync(StateHasChanged);
    }

    async Task OnReturn(InventoryTransferRequestVM data)
    {
        NavManager.NavigateTo(ITRRoutes.INDEX);
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

    private bool LinesNeedAssignment(InventoryTransferRequestVM data, out List<InventoryTransferRequestLineVM> lines)
    {
        lines = [.. data.Lines.Where(x => !x.IsAllAssigned)];
        return lines.Any();
    }
}
