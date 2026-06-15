using Application.UseCases.Queries.Transaction.StockTransferRequest;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.StockTransferRequest;

public class StockTransferRequestHandler(ISender sender) : IStockTransferRequestHandler
{
    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetInterCompanyTransferOrdersList(DataGridIntent intent)
    {

        GetIntercompanyTransferOrderListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), count);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetReturnsList(DataGridIntent intent)
    {
        GetReturnsListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), count);
    }

    public Task<StockTransferRequestInfoVM?> GetStockTransferRequest(string reference, bool includeLines = true)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<StockTransferRequestLineVM> data, int count)> GetStockTransferRequestLines(string reference, DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetStockTransferRequestsList(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetTransferOrdersList(DataGridIntent intent)
    {
        GetTransferOrderListQry query = new(intent);

        (var data, int count) = await sender.Send(query);

        return (data.Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), count);
    }
}
