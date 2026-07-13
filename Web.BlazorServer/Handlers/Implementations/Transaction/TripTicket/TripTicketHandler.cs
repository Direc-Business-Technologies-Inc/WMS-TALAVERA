using Application.DataTransferObjects.Transactions.TripTicket;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Commands.Transaction.TripTicket.NS;
using Application.UseCases.Queries.Others.NS;
using Application.UseCases.Queries.Transaction.TripTicket;
using Application.UseCases.Queries.Transaction.TripTicket.NS;
using Mapster;
using MediatR;
using Shared.Entities;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.TripTicket;

public class TripTicketHandler(ISender Sender) : ITripTicketHandler
{
    public async Task<(IEnumerable<TripTicketDataGridVM> Data, int Count)> GetTTDataGridAsync(DataGridIntent intent, int subsidiaryId)
    {
        GetTripTicketDataGridQry qry = new(intent, subsidiaryId);
        (IEnumerable<TripTicketDataGridDTO> Data, int Count) = await Sender.Send(qry);
        return (Data.Adapt<IEnumerable<TripTicketDataGridVM>>(), Count);
    }

    public async Task<TripTicketVM?> GetTripTicketAsync(int id)
    {
        GetTripTicketQry qry = new(id);
        TripTicketDataGridDTO? response = await Sender.Send(qry);

        if (response is null)
            return null;

        return new TripTicketVM
        {
            Id = response.NetsuiteTripTicketInternalId,
            TripDate = response.TripDate,
            Destinations = string.IsNullOrWhiteSpace(response.Destination)
                ? []
                : [new LocationVM { LocationName = response.Destination }],
            Driver = string.IsNullOrWhiteSpace(response.Driver)
                ? null
                : new DriverVM { FirstName = response.Driver },
            OriginLocation = string.IsNullOrWhiteSpace(response.Location)
                ? null
                : new LocationVM { LocationName = response.Location },
            Helper = string.IsNullOrWhiteSpace(response.HelperName) ?
                null :
                new HelperVM { NetsuiteEmployeeInternalId = response.HelperId, FirstName = response.HelperName },
            TruckPlateNumber = string.IsNullOrWhiteSpace(response.TruckPlateNumber) ?
                null :
                new TruckPlateNumberVM { NetsuiteTruckPlateNoInternalId = response.TruckPlateNumberId, TruckPlateNoName = response.TruckPlateNumber },
            ItemFulfillments = [.. await GetTripTicketFulfillmentsAsync(id)]
        };
    }

    public async Task<IEnumerable<ItemFulfillmentVM>> GetTripTicketFulfillmentsAsync(int id)
    {
        var response = await Sender.Send(new GetTripTicketFulfillmentsQry(id));
        return response.Adapt<IEnumerable<ItemFulfillmentVM>>();
    }

    public async Task<IEnumerable<ItemFulfillmentVM>> GetPackedItemFulfillmentsAsync()
    {
        var response = await Sender.Send(new GetPackedItemFulfillmentsQry());
        return response.Adapt<IEnumerable<ItemFulfillmentVM>>();
    }

    public async Task<IEnumerable<DriverVM>> GetDriversAsync()
    {
        var response = await Sender.Send(new GetDriversQry());
        return response.Adapt<IEnumerable<DriverVM>>();
    }

    public async Task<IEnumerable<HelperVM>> GetHelpersAsync()
    {
        var response = await Sender.Send(new GetHelpersQry());
        return response.Adapt<IEnumerable<HelperVM>>();
    }

    public async Task<IEnumerable<LocationVM>> GetLocationsAsync()
    {
        var response = await Sender.Send(new GetLocationsQry());
        return response.Adapt<IEnumerable<LocationVM>>();
    }

    public async Task<IEnumerable<TruckPlateNumberVM>> GetTruckPlateNumbersAsync()
    {
        var response = await Sender.Send(new GetTruckPlateNumbersQry());
        return response.Adapt<IEnumerable<TruckPlateNumberVM>>();
    }

    public async Task<bool> PostTripTicketAsync(TripTicketVM data)
    {
        PostTripTicketCmd cmd = new(data.Adapt<PostTripTicketDTO>());
        var result = await Sender.Send(cmd);
        return result.Success && result.Data == true;
    }
}
