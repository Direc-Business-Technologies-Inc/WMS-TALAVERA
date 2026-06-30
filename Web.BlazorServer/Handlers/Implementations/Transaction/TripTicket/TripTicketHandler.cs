using Application.DataTransferObjects.Transactions.TripTicket;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Commands.Transaction.TripTicket.NS;
using Application.UseCases.Queries.Transaction.TripTicket;
using Mapster;
using MediatR;
using Shared.Entities;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.TripTicket;

public class TripTicketHandler(ISender Sender) : ITripTicketHandler
{
    public async Task<(IEnumerable<TripTicketDataGridVM> Data, int Count)> GetTTDataGridAsync(DataGridIntent intent)
    {
        GetTripTicketDataGridQry qry = new(intent);
        (IEnumerable<TripTicketDataGridDTO> Data, int Count) = await Sender.Send(qry);
        return (Data.Adapt<IEnumerable<TripTicketDataGridVM>>(), Count);
    }

    public async Task<TripTicketDataGridVM?> GetTripTicketAsync(int id)
    {
        GetTripTicketQry qry = new(id);
        TripTicketDataGridDTO? response = await Sender.Send(qry);
        return response.Adapt<TripTicketDataGridVM?>();
    }

    public async Task<bool> PostTripTicketAsync(TripTicketVM data)
    {
        PostTripTicketCmd cmd = new(data.Adapt<PostTripTicketDTO>());
        var result = await Sender.Send(cmd);
        return result.Success && result.Data == true;
    }
}
