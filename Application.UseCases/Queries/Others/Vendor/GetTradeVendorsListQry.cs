using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Others.Vendor;

public record GetTradeVendorsListQry(DataGridIntent Intent) : IRequest<(IEnumerable<VendorDTO> Data, int Count)>;

public class GetTradeVendorsListQryHandler(IVendorIntegration vendorIntegration) : IRequestHandler<GetTradeVendorsListQry, (IEnumerable<VendorDTO> Data, int Count)>
{
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> Handle(GetTradeVendorsListQry request, CancellationToken cancellationToken)
    {
        return await vendorIntegration.GetTradeVendorsListAsync(request.Intent);
    }
}