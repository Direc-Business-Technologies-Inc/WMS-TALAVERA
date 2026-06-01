using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Commands.Transaction.GoodsReceipt;
using Application.UseCases.Queries.Transaction.GoodsReceipt;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReceipt;
using Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.GoodsReceipt;

public class GoodsReceiptHandler(ISender Sender) : IGoodsReceiptHandler
{
    public async Task<GoodsReceiptVM?> GetDraftGoodsReceiptAsync(int docEntry)
    {
        GetGoodsReceiptDraftQry qry = new(docEntry);
        GoodsReceiptDTO? response = await Sender.Send(qry);

        return response.Adapt<GoodsReceiptVM>();
    }

    public async Task<GoodsReceiptVM?> GetGoodsReceiptAsync(int docEntry)
    {
        GetGoodsReceiptQry qry = new(docEntry);
        GoodsReceiptDTO? response = await Sender.Send(qry);

        return response.Adapt<GoodsReceiptVM>();
    }

    public async Task<(IEnumerable<GoodsReceiptDataGridVM> Data, int Count)> GetGoodsReceiptDataGridAsync(DataGridIntent intent)
    {
        GetGoodsReceiptDataGridQry qry = new(intent);
        (IEnumerable<GoodsReceiptDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<GoodsReceiptDataGridVM>>(), Count);

    }

    public async Task<(IEnumerable<GoodsReceiptDataGridVM> Data, int Count)> GetGoodsReceiptDataGridAsync(DataGridIntent intent, string status)
    {
        GetGoodsReceiptDraftDataGridQry qry = new(intent, status);
        (IEnumerable<GoodsReceiptDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<GoodsReceiptDataGridVM>>(), Count);
    }

    public async Task<bool> PostGoodsReceiptAsync(GoodsReceiptVM data)
    {
        PostGoodsReceiptCmd cmd = new(data.Adapt<GoodsReceiptDTO>());
        bool result = await Sender.Send(cmd);

        return result;
    }
}
