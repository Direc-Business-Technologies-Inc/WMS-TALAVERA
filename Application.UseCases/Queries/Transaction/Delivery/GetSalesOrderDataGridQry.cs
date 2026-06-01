using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Mapster;
using MediatR;
using Shared.Entities;


namespace Application.UseCases.Queries.Transaction.Delivery;

public record GetSalesOrderDataGridQry(DataGridIntent Intent) : IRequest<(IEnumerable<SalesOrderDataGridDTO> Data, int Count)>;

public class GetSalesOrderDataGridQryHandler(
    IDeliveryIntegration deliveryIntegration)
    : IRequestHandler<GetSalesOrderDataGridQry, (IEnumerable<SalesOrderDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<SalesOrderDataGridDTO> Data, int Count)> Handle(GetSalesOrderDataGridQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<SalesOrderDataGridSAPDTO> Data, int Count) = await deliveryIntegration.GetSalesOrderDocumentsAsync(request.Intent);

        return (Data.Adapt<IEnumerable<SalesOrderDataGridDTO>>(), Count);
    }
}