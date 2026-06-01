using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Mapster;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.Others;

public record GetWarehousesQry(DataGridIntent Intent) : IRequest<(IEnumerable<WarehouseDTO> Data, int Count)>;

public class GetWarehousesQryHandler(
    IWarehouseMasterDataIntegration warehouseIntegration)
    : IRequestHandler<GetWarehousesQry, (IEnumerable<WarehouseDTO> Data, int Count)>
{
    public async Task<(IEnumerable<WarehouseDTO> Data, int Count)> Handle(GetWarehousesQry request, CancellationToken cancellationToken)
    {
        (IEnumerable<WarehouseSelectionSAPDTO> Data, int Count) = await warehouseIntegration.GetWarehouseAsync(request.Intent);

        return (Data.Adapt<IEnumerable<WarehouseDTO>>(), Count);
    }
}
