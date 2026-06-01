using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Transaction.GoodsReceipt;

public record GetGoodsReceiptDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<GoodsReceiptDataGridDTO> Data, int Count)>;

public class etGoodsReceiptDataGridQryHandler(
    IGoodsReceiptIntegration grIntegration) 
    : IRequestHandler<GetGoodsReceiptDataGridQry, (IEnumerable<GoodsReceiptDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<GoodsReceiptDataGridDTO> Data, int Count)> Handle(GetGoodsReceiptDataGridQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count) = await grIntegration.GetApprovedGoodsReceiptDataGrid(request.Intent);

        return (Data.Adapt<IEnumerable<GoodsReceiptDataGridDTO>>(), Count);
    }
}
