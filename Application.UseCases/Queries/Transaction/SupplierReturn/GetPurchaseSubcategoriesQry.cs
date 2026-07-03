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

public record GetPurchaseSubcategoriesQry(DataGridIntent Intent) : IRequest<(IEnumerable<PurchaseSubCategoryDTO> Data, int Count)>;
public class GetPurchaseSubcategoriesQryHandler(ISupplierReturnIntegration integration) 
    : IRequestHandler<GetPurchaseSubcategoriesQry, (IEnumerable<PurchaseSubCategoryDTO> Data, int Count)>
{
    public async Task<(IEnumerable<PurchaseSubCategoryDTO> Data, int Count)> Handle(GetPurchaseSubcategoriesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetPurchaseSubcategoriesAsync(request.Intent);
    }
}