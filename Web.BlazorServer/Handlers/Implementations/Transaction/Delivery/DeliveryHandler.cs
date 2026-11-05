using Application.DataTransferObjects.Transactions.Delivery;
using Application.UseCases.Commands.Transaction.Delivery;
using Application.UseCases.Queries.Transaction.Delivery;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;
using Web.BlazorServer.ViewModels.Transaction.Delivery;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.Delivery;

public class DeliveryHandler(
    ISender Sender)
    : IDeliveryHandler
{
    public async Task<(IEnumerable<SalesOrderDataGridVM> Data, int Count)> GetSalesOrderDataGridAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetSalesOrderDataGridQry qry = new(intent);
        (IEnumerable<SalesOrderDataGridDTO> Data, int Count) = await Sender.Send(qry, ct);
        return (Data.Adapt<IEnumerable<SalesOrderDataGridVM>>(), Count);
    }

    public async Task<SalesOrderVM?> GetSalesOrderAsync(int docEntry, CancellationToken ct = default)
    {
        GetSalesOrderQry qry = new(docEntry);
        SalesOrderDTO? response = await Sender.Send(qry, ct);
        return response.Adapt<SalesOrderVM?>();
    }

    public async Task<(IEnumerable<DeliveryDataGridVM> Data, int Count)> GetDeliveryDataGridAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        GetDeliveryDataGridQry qry = new(intent);
        (IEnumerable<DeliveryDataGridDTO> Data, int Count) = await Sender.Send(qry, ct);
        return (Data.Adapt<IEnumerable<DeliveryDataGridVM>>(), Count);
    }

    public async Task<DeliveryVM?> GetDeliveryAsync(int docEntry, CancellationToken ct = default)
    {
        GetDeliveryQry qry = new(docEntry);
        DeliveryDTO? response = await Sender.Send(qry, ct);
        return response.Adapt<DeliveryVM?>();
    }

    public async Task<bool> PostDeliveryAsync(DeliveryVM data, CancellationToken ct = default)
    {
        PostDeliveryCmd cmd = new(data.Adapt<DeliveryDTO>());
        bool result = await Sender.Send(cmd, ct);
        return result;
    }

    public async Task<IEnumerable<DeliveryMeansVM>> GetDeliveryMeansAsync(CancellationToken ct = default)
    {
        GetDeliveryMeansQry qry = new();
        IEnumerable<DeliveryMeansDTO> response = await Sender.Send(qry, ct);
        return response.Adapt<IEnumerable<DeliveryMeansVM>>();
    }
}
