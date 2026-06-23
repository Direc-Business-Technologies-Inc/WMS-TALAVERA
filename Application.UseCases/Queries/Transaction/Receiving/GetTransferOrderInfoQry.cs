using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Mapster;
using MediatR;
using Shared.Libraries.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;


public record GetTransferOrderInfoQry(string DocEntry) : IRequest<TransferOrderDTO?>;

public class GetTransferOrderInfoQryHandler(
    IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetTransferOrderInfoQry, TransferOrderDTO?>
{
    public async Task<TransferOrderDTO?> Handle(GetTransferOrderInfoQry request, CancellationToken cancellationToken)
    {
        TransferOrderDTO? headerResponse = await receivingIntegration.GetTransferOrderHeaderAsync(request.DocEntry);

        if (headerResponse is null) return null;

        return headerResponse.Adapt<TransferOrderDTO>();
    }
}
