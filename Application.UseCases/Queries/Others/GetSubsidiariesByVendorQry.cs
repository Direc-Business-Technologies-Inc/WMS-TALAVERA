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

public record GetSubsidiariesByVendorQry(DataGridIntent Intent, int vendorId) : IRequest<(IEnumerable<SubsidiaryDTO> Data, int Count)>;

public class GetSubsidiariesByVendorQryHandler(ISubsidiaryIntegration integration)
    : IRequestHandler<GetSubsidiariesByVendorQry, (IEnumerable<SubsidiaryDTO> Data, int Count)>
{
    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> Handle(GetSubsidiariesByVendorQry request, CancellationToken cancellationToken)
    {
        return integration.GetSubsidiariesByVendorAsync(request.Intent, request.vendorId);
    }
}
