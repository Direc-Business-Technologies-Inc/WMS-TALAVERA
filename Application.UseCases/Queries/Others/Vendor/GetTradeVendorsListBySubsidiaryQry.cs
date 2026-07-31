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


public record GetTradeVendorsListBySubsidiaryQry(DataGridIntent Intent, int subsidiaryId) : IRequest<(IEnumerable<VendorDTO> Data, int Count)>;

public class GetTradeVendorsListBySubsidiaryQryHandler(IVendorIntegration vendorIntegration) 
    : IRequestHandler<GetTradeVendorsListBySubsidiaryQry, (IEnumerable<VendorDTO> Data, int Count)>
{
    public async Task<(IEnumerable<VendorDTO> Data, int Count)> Handle(GetTradeVendorsListBySubsidiaryQry request, CancellationToken cancellationToken)
    {
        return await vendorIntegration.GetTradeVendorsBySubsidiaryListAsync(request.Intent, request.subsidiaryId);
    }
}