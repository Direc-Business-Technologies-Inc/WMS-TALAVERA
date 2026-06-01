using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.GoodsReceipt;

public record GetGoodsReceiptDraftDataGridQry(DataGridIntent Intent, string Status) : IRequest<(IEnumerable<GoodsReceiptDataGridDTO> Data, int Count)>;

public class GetGoodsReceiptDraftDataGridQryHandler(
    IGoodsReceiptIntegration grIntegration)
    : IRequestHandler<GetGoodsReceiptDraftDataGridQry, (IEnumerable<GoodsReceiptDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<GoodsReceiptDataGridDTO> Data, int Count)> Handle(GetGoodsReceiptDraftDataGridQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count) = await grIntegration.GetGoodsReceiptDraftDataGrid(request.Intent, request.Status);
        return (Data.Adapt<IEnumerable<GoodsReceiptDataGridDTO>>(), Count);
    }
}
