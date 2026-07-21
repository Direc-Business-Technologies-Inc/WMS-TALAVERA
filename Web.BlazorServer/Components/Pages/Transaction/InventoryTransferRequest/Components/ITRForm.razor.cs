using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest.Components;

public partial class ITRForm
{
    [Inject] ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] IItemsHandler itemsHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ICustomerHandler customerHandler { get; set; } = default!;
    [Parameter][EditorRequired] public InventoryTransferRequestVM Model { get; set; }
    [Parameter][EditorRequired] public EditContext EditContext { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnSubmit { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnReturn { get; set; }
    [Parameter] public EventCallback<InventoryTransferRequestVM> OnSecondaryAction { get; set; }
    [Parameter] public string ReturnLabel { get; set; } = "Return";
    [Parameter] public string SubmitLabel { get; set; } = "Submit";
    [Parameter] public string SecondaryActionLabel { get; set; } = "Action";
    [Parameter] public bool ReadOnly { get; set; } = false;
    [Parameter] public bool EditMode { get; set; } = false;
    [Parameter] public bool Disabled { get; set; } = false;

    QuickVirtualizedDropdown<LocationVM> SourceLocationDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<LocationVM> DestinationLocationDropdown { get; set; } = default!;
    QuickVirtualizedDropdown<SubsidiaryVM> SubsidiaryDropdown { get; set; } = default!;

    readonly List<AppFilterDescriptor> ItemFilters = [
        DataGridFilterUtilities.GreaterThan("QuantityOnHand", 0)
    ];

    async Task<(IEnumerable<CustomerVM>, int)> CustomerProvider(DataGridIntent intent)
    {
        return await customerHandler.GetCustomersListAsync(intent);
    }
    async Task<(IEnumerable<SubsidiaryVM>, int)> SubsidiaryProvider(DataGridIntent intent)
    {
        return await subsidiaryHandler.GetSubsidiariesAsync(intent);
    }
    async Task<(IEnumerable<LocationVM>, int)> SourceLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }
    async Task<(IEnumerable<LocationVM>, int)> DestinationLocationProvider(DataGridIntent intent)
    {
        if (Model.Subsidiary is null) return ([], 0);

        return await locationHandler.GetLocationsBySubsidiaryAsync(intent, Model.Subsidiary.Id);
    }
    async Task<(IEnumerable<ItemUnitVM>, int)> ItemUnitProvider(DataGridIntent intent, int itemId)
    {
        return await itemsHandler.GetItemUnits(itemId, intent);
    }

    async Task AddItems(List<ItemsVM> items)
    {
        Model.Lines.AddRange(items.Select(x => new InventoryTransferRequestLineVM
        {
            ItemID = x.Id,
            ItemCode = x.Name,
            ItemDescription = x.Description,
            UsesBins = x.UsesBins,
            UoM = x.StockUnit,
            QuantityOnHand = x.QuantityOnHand,
            Location = Model.SourceLocation
        }));
    }

    async Task SecondaryAction()
    {
        if (OnSecondaryAction.HasDelegate) await OnSecondaryAction.InvokeAsync(Model);
    }

    async Task Return()
    {
        if (OnReturn.HasDelegate) await OnReturn.InvokeAsync(Model);
    }

    async Task Submit()
    {
        if (OnSubmit.HasDelegate) await OnSubmit.InvokeAsync(Model);
    }
    async Task SubsidiarySet(SubsidiaryVM? value)
    {
        var oldValue = Model.Subsidiary;
        Model.Subsidiary = value;
        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing subsidiaries will clear added items", "Change Subsidiaries?");
            await Task.Yield();
            Model.Subsidiary = oldValue;
            if (!response) return;
        }

        Model.Lines.Clear();

        await Task.WhenAll(
            LocationSet(null),
            DestinationLocationSet(null)
        );


        SourceLocationDropdown.Reset();
        DestinationLocationDropdown.Reset();

        await InvokeAsync(StateHasChanged);
    }

    async Task RemoveLine(InventoryTransferRequestLineVM line)
    {
        Model.Lines.Remove(line);

        await InvokeAsync(StateHasChanged);
    }

    async Task LocationSet(LocationVM? value)
    {
        var oldValue = Model.SourceLocation;
        Model.SourceLocation = value;

        if (Model.Lines.Count > 0)
        {
            var response = await AlertService.PromptAsync("Changing source location will clear added items", "Change Source Location?");
            if (!response)
            {
                await Task.Yield();
                Model.SourceLocation = oldValue;
                return;
            }
        }

        if (_areEqual(Model.DestinationLocation, value))
        {
            ToastService.Error("Destination warehouse cannot be the same as the source warehouse");
            await Task.Yield();
            Model.SourceLocation = oldValue;
            return;
        }

        Model.Lines.Clear();
        await InvokeAsync(StateHasChanged);
    }

    async Task DestinationLocationSet(LocationVM? value)
    {
        var oldValue = Model.DestinationLocation;
        Model.DestinationLocation = value;

        if (_areEqual(Model.SourceLocation, value))
        {
            ToastService.Error("Destination warehouse cannot be the same as the source warehouse");
            await Task.Yield();
            Model.DestinationLocation = oldValue;
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    async Task SetLineUoM(InventoryTransferRequestLineVM line, ItemUnitVM? uom)
    {
        decimal oldcr = line.UoM?.ConversionRate ?? 1;
        decimal newcr = uom?.ConversionRate ?? 1;

        line.QuantityAlloted *= oldcr / newcr;

        line.UoM = uom;
        await InvokeAsync(StateHasChanged);
    }

    bool _areEqual(LocationVM? a, LocationVM? b) => (a is null || b is null) ? false : a?.Id == b?.Id;
}
