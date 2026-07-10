using Mobile.MAUI.Services;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Authentication;
using Shared.Libraries.ViewModel.TripTicket;
using System.Text.Json;

namespace Mobile.MAUI.Components.Pages.TripTicket;

public partial class TripTicketDetailsView
{
    [Inject]
    DialogService DialogService { get; set; }

    AppAction<List<LocationVM>> ActionGetDestinations;
    AppAction<List<LocationVM>> ActionGetOriginLocations;
    AppAction<List<DriverVM>> ActionGetDrivers;
    AppAction<List<HelperVM>> ActionGetHelpers;
    AppAction<List<TruckPlateNumberVM>> ActionGetTruckPlateNumbers;

    List<LocationVM> Destination { get; set; } = [];
    List<DriverVM> Drivers { get; set; } = [];
    List<HelperVM> Helpers { get; set; } = [];
    List<TruckPlateNumberVM> TruckPlateNumbers { get; set; } = [];
    List<LocationVM> OriginLocations { get; set; } = [];

    public TripTicketVM Model { get; set; } = new();

    int UserSubsidiaryId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        ActionGetDestinations = new AppAction<List<LocationVM>>
        {
            Name = "GetDestinations",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<LocationVM>>("/Lookup/Locations");
                return res;
            },
            OnSuccess = async (result) =>
            {
                Destination = result.Data.Select(line => new LocationVM
                {
                    LocationName = line.LocationName,
                    NetsuiteLocationInternalId = line.NetsuiteLocationInternalId
                }).ToList() ?? [];

                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetOriginLocations = new AppAction<List<LocationVM>>
        {
            Name = "GetOriginLocations",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<LocationVM>>("/Lookup/Susidiary/Locations", new { NetsuiteUserSubsidiaryInternalId = UserSubsidiaryId });
                return res;
            },
            OnSuccess = async (result) =>
            {
                OriginLocations = result.Data.Select(line => new LocationVM
                {
                    LocationName = line.LocationName,
                    NetsuiteLocationInternalId = line.NetsuiteLocationInternalId
                }).ToList() ?? [];

                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetDrivers = new AppAction<List<DriverVM>>
        {
            Name = "GetDrivers",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<DriverVM>>("/Lookup/Drivers");
                return res;
            },
            OnSuccess = async (result) =>
            {
                Drivers = result.Data ?? new();
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetHelpers = new AppAction<List<HelperVM>>
        {
            Name = "GetHelpers",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<HelperVM>>("/Lookup/Helpers");
                return res;
            },
            OnSuccess = async (result) =>
            {
                Helpers = result.Data ?? new();
                await InvokeAsync(StateHasChanged);
            }
        };

        ActionGetTruckPlateNumbers = new AppAction<List<TruckPlateNumberVM>>
        {
            Name = "GetTruckPlateNumbers",
            TaskAsync = async () =>
            {
                await InvokeAsync(StateHasChanged);
                var res = await Client.Get<List<TruckPlateNumberVM>>("/Lookup/TruckPlateNumbers");
                return res;
            },
            OnSuccess = async (result) =>
            {
                TruckPlateNumbers = result.Data ?? new();
                await InvokeAsync(StateHasChanged);
            }
        };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string? userAuth = await SecureStorage.GetAsync("UserAuth");
            if (userAuth is not null)
            {
                var auth = JsonSerializer.Deserialize<AuthenticationVM>(userAuth);

                UserSubsidiaryId = auth.NetsuiteSubsidiaryInternalId;
            }

            await ActionFactory.ExecuteAppActionAsync(ActionGetDestinations);
            await ActionFactory.ExecuteAppActionAsync(ActionGetDrivers);
            await ActionFactory.ExecuteAppActionAsync(ActionGetHelpers);
            await ActionFactory.ExecuteAppActionAsync(ActionGetTruckPlateNumbers);
            await ActionFactory.ExecuteAppActionAsync(ActionGetOriginLocations);
        }
    }

    private void OnConfirm()
    {
        var result = new TripTicketVM
        {
            Destinations = Model.Destinations,
            Driver = Model.Driver,
            Helper = Model.Helper,
            TruckPlateNumber = Model.TruckPlateNumber,
            OriginLocation = Model.OriginLocation,
            TripDate = Model.TripDate
        };

        DialogService.Close(result);
    }

    private async Task OnCancel()
    {
        DialogService.Close(null);
    }

}