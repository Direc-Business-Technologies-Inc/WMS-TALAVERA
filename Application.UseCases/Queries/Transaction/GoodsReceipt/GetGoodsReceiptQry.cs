using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsReceipt;

public record GetGoodsReceiptQry(int DocEntry) : IRequest<GoodsReceiptDTO?>;

public class GetGoodsReceiptQryHandler(
    IGoodsReceiptIntegration grIntegration) 
    : IRequestHandler<GetGoodsReceiptQry, GoodsReceiptDTO?>
{
    public async Task<GoodsReceiptDTO?> Handle(GetGoodsReceiptQry request, CancellationToken cancellationToken)
    {
        GoodsReceiptHeaderSAPDTO? gr = await grIntegration.GetGoodsReceiptHeader(request.DocEntry);
        if (gr is null)
            return null;

        IEnumerable<GoodsReceiptLineSAPDTO> grLines = await grIntegration.GetGoodsReceiptLines(request.DocEntry);

        GoodsReceiptDTO dto = gr.Adapt<GoodsReceiptDTO>();
        dto.DocumentLines = [.. grLines.Adapt<IEnumerable<GoodsReceiptLineDTO>>()];

        return dto;
    }
}