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

public record GetVendorsListQry(DataGridIntent Intent) : IRequest<(IEnumerable<VendorDTO> Data, int Count)>;

public class GetVendorsListQryHandler(IVendorIntegration vendorIntegration) : IRequestHandler<GetVendorsListQry, (IEnumerable<VendorDTO> Data, int Count)>
{
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> Handle(GetVendorsListQry request, CancellationToken cancellationToken)
    {
        return await vendorIntegration.GetVendorsListAsync(request.Intent);
    }
}