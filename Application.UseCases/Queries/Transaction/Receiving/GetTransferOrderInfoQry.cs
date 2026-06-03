using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;


public record GetTransferOrderInfoQry(int DocEntry) : IRequest<ReceivingInfoDTO?>;

public class GetTransferOrderInfoQryHandler(
    IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetTransferOrderInfoQry, ReceivingInfoDTO?>
{
    public async Task<ReceivingInfoDTO?> Handle(GetTransferOrderInfoQry request, CancellationToken cancellationToken)
    {
        ReceivingInfoNSDTO? headerResponse = await receivingIntegration.GetTransferOrderHeaderAsync(request.DocEntry);

        if (headerResponse is null) return null;

        return headerResponse.Adapt<ReceivingInfoDTO>();
    }
}
