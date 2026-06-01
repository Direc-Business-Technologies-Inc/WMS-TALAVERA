using Application.DataTransferObjects.Others.SAP;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IWarehouseMasterDataIntegration
{
    Task<(IEnumerable<WarehouseSelectionSAPDTO> Data, int Count)> GetWarehouseAsync(DataGridIntent intent);
}
