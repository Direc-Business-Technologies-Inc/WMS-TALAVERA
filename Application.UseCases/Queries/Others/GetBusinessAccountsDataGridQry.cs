using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others;

public record GetBusinessAccountsDataGridQry(DataGridIntent Intent, int? subsidiary = null) : IRequest<(IEnumerable<BusinessAccountDTO> data, int count)>;

public class GetBusinessAccountsDataGridQryHandler(
        IBusinessAccountIntegration integration
    ) : IRequestHandler<GetBusinessAccountsDataGridQry, (IEnumerable<BusinessAccountDTO> data, int count)>
{
    public async Task<(IEnumerable<BusinessAccountDTO> data, int count)> Handle(GetBusinessAccountsDataGridQry request, CancellationToken cancellationToken)
    {
        return await integration.GetBusinessAccountsAsync(request.Intent, request.subsidiary);
    }
}