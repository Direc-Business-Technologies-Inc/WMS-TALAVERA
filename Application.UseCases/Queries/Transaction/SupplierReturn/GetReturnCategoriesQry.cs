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

public record GetReturnCategoriesQry(DataGridIntent Intent) : IRequest<(IEnumerable<ReturnCategoryDTO> data, int count)>;

public class GetReturnCategoriesQryHandler(ISupplierReturnIntegration integration)
    : IRequestHandler<GetReturnCategoriesQry, (IEnumerable<ReturnCategoryDTO> data, int count)>
{
    public async Task<(IEnumerable<ReturnCategoryDTO> data, int count)> Handle(GetReturnCategoriesQry request, CancellationToken cancellationToken)
    {
        return await integration.GetReturnCategories(request.Intent);
    }
}