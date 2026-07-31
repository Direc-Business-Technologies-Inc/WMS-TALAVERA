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

public record GetVendorsListBySubsidiaryQry(DataGridIntent Intent, int subsidiaryId) : IRequest<(IEnumerable<VendorDTO> Data, int Count)>;

public class GetVendorsListBySubsidiaryQryHandler(IVendorIntegration vendorIntegration) : IRequestHandler<GetVendorsListBySubsidiaryQry, (IEnumerable<VendorDTO> Data, int Count)>
{
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> Handle(GetVendorsListBySubsidiaryQry request, CancellationToken cancellationToken)
    {
        return await vendorIntegration.GetVendorsBySubsidiaryListAsync(request.Intent, request.subsidiaryId);
    }
}