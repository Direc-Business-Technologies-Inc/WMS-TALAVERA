using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Transaction.GoodsReceipt;

public record GetGoodsReceiptDraftQry(int DocEntry) : IRequest<GoodsReceiptDTO?>;

public class GetGoodsReceiptDraftQryHandler(
    IGoodsReceiptIntegration grIntegration
    ) 
    : IRequestHandler<GetGoodsReceiptDraftQry, GoodsReceiptDTO?>
{
    public async Task<GoodsReceiptDTO?> Handle(GetGoodsReceiptDraftQry request, CancellationToken cancellationToken)
    {
        GoodsReceiptHeaderSAPDTO? grDraft = await grIntegration.GetGoodsReceiptDraftHeader(request.DocEntry);
        if(grDraft is null)
            return null;

        IEnumerable<GoodsReceiptLineSAPDTO> grDraftLines = await grIntegration.GetGoodsReceiptDraftLines(request.DocEntry);

        GoodsReceiptDTO dto = grDraft.Adapt<GoodsReceiptDTO>();
        dto.DocumentLines = [.. grDraftLines.Adapt<IEnumerable<GoodsReceiptLineDTO>>()];

        return dto;
    }
}

