using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Transaction.SupplierReturn;

public record GetPurchaseCategoriesQry(DataGridIntent Intent) : IRequest<(IEnumerable<PurchaseCategoryDTO> Data, int Count)>;

public class GetPurchaseCategoriesQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetPurchaseCategoriesQry, (IEnumerable<PurchaseCategoryDTO> Data, int Count)>
{
    public async Task<(IEnumerable<PurchaseCategoryDTO> Data, int Count)> Handle(GetPurchaseCategoriesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetPurchaseCategoriesAsync(request.Intent);
    }
}   
