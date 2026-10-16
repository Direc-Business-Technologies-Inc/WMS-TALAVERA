using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Application.DataTransferObjects.Others.NS;
using Mapster;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.Receiving;


public record GetTransferOrderLinesQry(string DocEntry, DataGridIntent Intent) : IRequest<(IEnumerable<ReceivingLineDTO> data, int count)>;

public class GetTransferOrderLinesQryHandler(
    IReceivingIntegration receivingIntegration)
    : IRequestHandler<GetTransferOrderLinesQry, (IEnumerable<ReceivingLineDTO> data, int count)>
{
    public async Task<(IEnumerable<ReceivingLineDTO> data, int count)> Handle(GetTransferOrderLinesQry request, CancellationToken cancellationToken)
    {
        try
        {
            var (nsData, count) = await receivingIntegration.GetTransferOrderLinesAsync(request.DocEntry, request.Intent);

            return (nsData.Adapt<IEnumerable<ReceivingLineDTO>>(), count);
        }
        catch (NetSuiteOffsetCorrectedException<IEnumerable<ReceivingLineNSDTO>> ex)
        {
            // The integration already recovered the legal page; translate the row
            // type and keep the corrected skip flowing so the grid can reposition.
            var adapted = ex.Rows.Adapt<IEnumerable<ReceivingLineDTO>>();
            throw new NetSuiteOffsetCorrectedException<IEnumerable<ReceivingLineDTO>>(
                adapted, ex.TotalResults, ex.CorrectedSkip, (Exception)ex.InnerException!);
        }
    }
}
