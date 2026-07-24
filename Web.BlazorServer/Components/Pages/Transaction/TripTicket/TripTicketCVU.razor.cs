using Mapster;
using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.ItemFulfillment;
using Shared.Services.Repository;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket;

partial class TripTicketCVU
{
    [SupplyParameterFromQuery]
    [Parameter] public int Ref { get; set; }

    #region Injects
    [Inject] ITripTicketHandler TripTicketHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] ILocationHandler locationHandler { get; set; } = default!;
    [Inject] ICurrentUserService currentUser { get; set; } = default!; 
    #endregion Injects

    PageActionTypeEnum PageAction { get; set; }
    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsLoadingData => AppBusyService.IsBusy(ActionView);
    bool IsLoadingFulfillments => AppBusyService.IsBusy(ActionGetFulfillments);

    readonly string ActionView = EnumHelper.GetEnumDescription(AppActions.ViewTripTicket);
    readonly string ActionCreate = EnumHelper.GetEnumDescription(AppActions.CreateTripTicket);
    readonly string ActionGetFulfillments = EnumHelper.GetEnumDescription(AppActions.GetPackedTripTicketFulfillments);
    readonly string ActionGetDrivers = EnumHelper.GetEnumDescription(AppActions.GetTripTicketDrivers);
    readonly string ActionGetHelpers = EnumHelper.GetEnumDescription(AppActions.GetTripTicketHelpers);
    readonly string ActionGetLocations = EnumHelper.GetEnumDescription(AppActions.GetTripTicketLocations);
    readonly string ActionGetTruckPlateNumbers = EnumHelper.GetEnumDescription(AppActions.GetTripTicketTruckPlateNumbers);

    AppTable<ItemFulfillmentVM> FulfillmentLinesTable { get; set; } = default!;
    DataGridSettings FulfillmentLinesTableSettings { get; set; } = new();
    List<ItemFulfillmentVM> PackedFulfillments { get; set; } = [];
    List<DriverVM> Drivers { get; set; } = [];
    List<HelperVM> Helpers { get; set; } = [];
    List<LocationVM> Locations { get; set; } = [];
    List<LocationVM> DestinationLocations { get; set; } = [];
    List<TruckPlateNumberVM> TruckPlateNumbers { get; set; } = [];

    const string PRINTABLE_URL = "https://11608969.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1671&deploy=1&compid=11608969&ns-at=AAEJ7tMQ9evIwFEEUifIBokQgQ0jhowAItpfjv5Smu7B76K41lU&recordType=customrecord_dbti_trip_ticket&transactionDefault=false";
    List<NavigationRouteVM> AdditionalRoutes { get; set; } =
    [
        new()
        {
            Name = "Trip Ticket",
            Position = 0,
            Icon = "transit_ticket",
            Uri = TripTicketRoutes.Root
        }
    ];

    protected override void OnParametersSet()
    {
        PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Creating)
            FormData.TripDate ??= DateTime.Today;

        if (Viewing)
            AppBusyService.SetBusy(ActionView, true);
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

    protected override async Task InitializeEditing()
    {
        if (Ref <= 0)
        {
            NavError("Please select a trip ticket from the list.");
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionView, true);
            return await TripTicketHandler.GetTripTicketAsync(Ref);
        }, AppActionOptionPresets.Loading(ActionView));

        AppBusyService.SetBusy(ActionView, false);

        action.OnSuccess(async result =>
        {
            if (result is null)
            {
                NavError($"Trip Ticket \"{Ref}\" could not be found.");
                return;
            }

            result.Adapt(FormData);
            AdaptToClone();
            await ResetFormContext();
            UnsavedChangesService.MarkClean();
        });

        action.OnFailure(ex =>
        {
            NavError(ex.Message);
            return Task.CompletedTask;
        });
    }

    protected override async Task CancelEditing()
    {
        AdaptToForm();
        await ResetFormContext();
        await GoBack();
    }

    protected override async Task HandleSubmit()
    {
        if (!ValidateFormData())
            return;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreate, true);
            var result =  await TripTicketHandler.PostTripTicketAsync(FormData);
            if (!result) throw new Exception("Failed to create Trip Ticket.");
            return result;
        }, AppActionOptionPresets.Confirmed(ActionCreate));

        AppBusyService.SetBusy(ActionCreate, false);

        action.OnSuccess(result =>
        {
            if (!result)
            {
                ToastService.Error("Failed to create Trip Ticket.");
                return Task.CompletedTask;
            }

            UnsavedChangesService.MarkClean();
            NavManager.NavigateTo(TripTicketRoutes.Root, true);
            return Task.CompletedTask;
        });
    }

    #region Custom Functions
    async Task LoadDataAsync()
    {
        GridSettingsLoaded = true;

        if (Creating)
        {
            await LoadPackedFulfillmentsAsync();
            await LoadDriversAsync();
            await LoadHelpersAsync();
            await LoadLocationsAsync();
            await LoadDestLocationsAsync();
            await LoadTruckPlateNumbersAsync();
            AdaptToClone();
            UnsavedChangesService.MarkClean();
        }

        if (Viewing)
            await InitializeEditing();

        AppBusyService.SetBusy(ActionView, false);
        await InvokeAsync(StateHasChanged);
    }

    async Task LoadPackedFulfillmentsAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetFulfillments, true);
            return await TripTicketHandler.GetPackedItemFulfillmentsAsync();
        }, AppActionOptionPresets.Loading(ActionGetFulfillments));

        AppBusyService.SetBusy(ActionGetFulfillments, false);
        action.OnSuccess(result =>
        {
            PackedFulfillments = result is null ? [] : [.. result];
            return Task.CompletedTask;
        });
    }

    async Task LoadDriversAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetDrivers, true);
            return await TripTicketHandler.GetDriversAsync();
        }, AppActionOptionPresets.Loading(ActionGetDrivers));

        AppBusyService.SetBusy(ActionGetDrivers, false);
        action.OnSuccess(result =>
        {
            Drivers = result is null ? [] : [.. result];
            return Task.CompletedTask;
        });
    }

    async Task LoadHelpersAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetHelpers, true);
            return await TripTicketHandler.GetHelpersAsync();
        }, AppActionOptionPresets.Loading(ActionGetHelpers));

        AppBusyService.SetBusy(ActionGetHelpers, false);
        action.OnSuccess(result =>
        {
            Helpers = result is null ? [] : [.. result];
            return Task.CompletedTask;
        });
    }

    async Task LoadLocationsAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetLocations, true);

            var userSubsidiary = CurrentUserService.NsSubsidiaryId;

            return await locationHandler.GetLocationsBySubsidiaryAsync(new() { Take = -1 }, userSubsidiary);

        }, AppActionOptionPresets.Loading(ActionGetLocations));

        AppBusyService.SetBusy(ActionGetLocations, false);
        action.OnSuccess(result =>
        {
            Locations = result.Count == 0 ? [] : [.. result.Data.Select(x => new LocationVM {
                NetsuiteLocationInternalId = x.Id,
                LocationName = x.Name,
            })];
            return Task.CompletedTask;
        });
    }

    async Task LoadDestLocationsAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetLocations, true);

            return await locationHandler.GetLocationsAsync(new() { Take = -1 });

        }, AppActionOptionPresets.Loading(ActionGetLocations));

        AppBusyService.SetBusy(ActionGetLocations, false);
        action.OnSuccess(result =>
        {
            DestinationLocations = result.Count == 0 ? [] : [.. result.Data.Select(x => new LocationVM {
                NetsuiteLocationInternalId = x.Id,
                LocationName = x.Name,
            })];
            return Task.CompletedTask;
        });
    }


    async Task LoadTruckPlateNumbersAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetTruckPlateNumbers, true);
            return await TripTicketHandler.GetTruckPlateNumbersAsync();
        }, AppActionOptionPresets.Loading(ActionGetTruckPlateNumbers));

        AppBusyService.SetBusy(ActionGetTruckPlateNumbers, false);
        action.OnSuccess(result =>
        {
            TruckPlateNumbers = result is null ? [] : [.. result];
            return Task.CompletedTask;
        });
    }

    bool ValidateFormData()
    {
        if (!FormData.TripDate.HasValue)
        {
            ToastService.Warning("Trip Date is required.");
            return false;
        }

        if (!FormData.Destinations.Any(x => x.NetsuiteLocationInternalId > 0))
        {
            ToastService.Warning("Please select at least one destination.");
            return false;
        }

        if (FormData.Driver is null || FormData.Driver.NetsuiteEmployeeInternalId <= 0)
        {
            ToastService.Warning("Driver is required.");
            return false;
        }

        if (FormData.Helper is null || FormData.Helper.NetsuiteEmployeeInternalId <= 0)
        {
            ToastService.Warning("Helper is required.");
            return false;
        }

        if (FormData.OriginLocation is null || FormData.OriginLocation.NetsuiteLocationInternalId <= 0)
        {
            ToastService.Warning("Location is required.");
            return false;
        }

        if (FormData.TruckPlateNumber is null || FormData.TruckPlateNumber.NetsuiteTruckPlateNoInternalId <= 0)
        {
            ToastService.Warning("Truck Plate No. is required.");
            return false;
        }

        if (!FormData.ItemFulfillments.Any(x => x.NetsuiteOrderInternalId > 0))
        {
            ToastService.Warning("Please add at least one fulfillment before submitting.");
            return false;
        }

        return true;
    }

    async Task RemoveFulfillment(ItemFulfillmentVM line)
    {
        FormData.ItemFulfillments =
        [
            .. FormData.ItemFulfillments.Where(x => x.NetsuiteOrderInternalId != line.NetsuiteOrderInternalId)
        ];

        OnFieldChanged(nameof(FormData.ItemFulfillments));

        if (FulfillmentLinesTable is not null)
            await FulfillmentLinesTable.DataGrid.Reload();
    }

    async Task GoBack()
    {
        if (UnsavedChangesService.HasChanges && Creating)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Trip Ticket Creation"))
                return;

        NavManager.NavigateTo(TripTicketRoutes.Root, true);
    }

    string GetEmployeeName(EmployeeVM? employee) =>
        employee is null
            ? string.Empty
            : employee.FullName.Trim();

    string PrintableURL => $"{PRINTABLE_URL}&recordId={FormData.Id}";

    string GetLocationName(LocationVM? location) =>
        location?.LocationName ?? string.Empty;

    string GetDestinationsText() =>
        string.Join(", ", FormData.Destinations
            .Select(x => x.LocationName)
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    void NavError(string message)
    {
        ToastService.Error(message);
        NavManager.NavigateTo(TripTicketRoutes.Root, true);
    }
    #endregion Custom Functions
}
