using Application.DataTransferObjects.Others.Inventory;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class InventoryIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IInventoryIntegration
{
    public async Task<(IEnumerable<InventoryBalanceDTO>, int)> GetInventoryBalance(DataGridIntent intent)
    {

        var query = builderFactory.Create()
            .Select(
                ("ib.item", nameof(InventoryBalanceNSDTO.ItemId)),
                ("ib.binnumber", nameof(InventoryBalanceNSDTO.BinNumber)),
                ("ib.quantityonhand", nameof(InventoryBalanceNSDTO.QuantityOnHand)),
                ("ib.quantitypicked", nameof(InventoryBalanceNSDTO.QuantityCommited)),
                ("is.name", nameof(InventoryBalanceNSDTO.StatusName)),
                ("is.id", nameof(InventoryBalanceNSDTO.StatusId))
            )
            .From("inventorybalance ib")
            .Join("inventorystatus is", "ib.inventorystatus = is.id")
            .WithDatagridIntent(intent)
            .Build();
        var response = await query.ExecuteWithPaging<InventoryBalanceNSDTO>(netsuiteService);
        return (response.items.Select(ConvertInventoryBalance), response.totalResults);
    }

    public async Task<(IEnumerable<InventoryStatusDTO>, int)> GetInventoryStatus(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(InventoryStatusDTO.Id)),
                ("name", nameof(InventoryStatusDTO.Name))
            )
            .From("inventorystatus")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<InventoryStatusDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    private InventoryBalanceDTO ConvertInventoryBalance(InventoryBalanceNSDTO nsdto) => nsdto.Adapt(new InventoryBalanceDTO()
    {
        Status = new InventoryStatusDTO
        {
            Name = nsdto.StatusName,
            Id = nsdto.StatusId
        }
    });

    private class InventoryBalanceNSDTO
    {

        public int ItemId { get; set; }
        public int BinNumber { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityCommited { get; set; }
    }
}
