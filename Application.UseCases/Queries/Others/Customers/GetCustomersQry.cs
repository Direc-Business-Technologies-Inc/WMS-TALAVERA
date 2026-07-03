using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.Customers;

public record GetCustomersQry(DataGridIntent intent) : IRequest<(IEnumerable<CustomerDTO>, int)>;

public class GetCustomersQryHandler(ICustomerIntegration integration)
    : IRequestHandler<GetCustomersQry, (IEnumerable<CustomerDTO>, int)>
{
    public async Task<(IEnumerable<CustomerDTO>, int)> Handle(GetCustomersQry request, CancellationToken cancellationToken)
    {
        return await integration.GetCustomersListAsync(request.intent);
    }
}
