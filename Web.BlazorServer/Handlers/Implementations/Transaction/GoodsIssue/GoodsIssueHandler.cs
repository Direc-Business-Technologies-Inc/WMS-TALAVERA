using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.UseCases.Commands.Transaction.GoodsIssue;
using Application.UseCases.Queries.Transaction.GoodsIssue;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsIssue;
using Web.BlazorServer.ViewModels.Transaction.GoodsIssue;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.GoodsIssue;

public class GoodsIssueHandler(ISender Sender) : IGoodsIssueHandler
{
    public async Task<GoodsIssueVM?> GetDraftGoodsIssueAsync(int docEntry)
    {
        GetGoodsIssueDraftQry qry = new(docEntry);
        GoodsIssueDTO? response = await Sender.Send(qry);

        return response.Adapt<GoodsIssueVM>();
    }

    public async Task<GoodsIssueVM?> GetGoodsIssueAsync(int docEntry)
    {
        GetGoodsIssueQry qry = new(docEntry);
        GoodsIssueDTO? response = await Sender.Send(qry);

        return response.Adapt<GoodsIssueVM>();
    }

    public async Task<(IEnumerable<GoodsIssueDataGridVM> Data, int Count)> GetGoodsIssueDataGridAsync(DataGridIntent intent)
    {
        GetGoodsIssueDataGridQry qry = new(intent);
        (IEnumerable<GoodsIssueDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<GoodsIssueDataGridVM>>(), Count);

    }

    public async Task<(IEnumerable<GoodsIssueDataGridVM> Data, int Count)> GetGoodsIssueDataGridAsync(DataGridIntent intent, string status)
    {
        GetGoodsIssueDraftDataGridQry qry = new(intent, status);
        (IEnumerable<GoodsIssueDataGridDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<GoodsIssueDataGridVM>>(), Count);
    }

    public async Task<bool> PostGoodsIssueAsync(GoodsIssueVM data)
    {
        PostGoodsIssueCmd cmd = new(data.Adapt<GoodsIssueDTO>());
        bool result = await Sender.Send(cmd);

        return result;
    }
}
