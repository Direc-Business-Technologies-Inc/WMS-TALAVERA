using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class WarehouseMasterDataIntegration : IWarehouseMasterDataIntegration
{
    public Task<(IEnumerable<WarehouseSelectionSAPDTO> Data, int Count)> GetWarehouseAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }
}
