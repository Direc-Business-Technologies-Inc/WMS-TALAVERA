using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.Inventory;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
using Shared.Entities;
using Shared.Libraries.Utilities;
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
                ("ib.binnumber", nameof(InventoryBalanceNSDTO.BinId)),
                ("b.binnumber", nameof(InventoryBalanceNSDTO.BinName)),
                ("ib.location", nameof(InventoryBalanceNSDTO.LocationId)),
                ("loc.name", nameof(InventoryBalanceNSDTO.LocationName)),
                ("ib.quantityavailable", nameof(InventoryBalanceNSDTO.QuantityOnHand)), // Change to quantityavailable to reflect the actual available quantity instead of on hand
                ("ib.quantitypicked", nameof(InventoryBalanceNSDTO.QuantityCommited)),
                ("is.name", nameof(InventoryBalanceNSDTO.StatusName)),
                ("is.id", nameof(InventoryBalanceNSDTO.StatusId))
            )
            .From("inventorybalance ib")
            .Join("inventorystatus is", "ib.inventorystatus = is.id")
            .Join("location loc", "ib.location = loc.id")
            .LeftJoin("bin b", "b.id = ib.binnumber")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<InventoryBalanceNSDTO>(netsuiteService);
        return (response.items.Select(ConvertInventoryBalance), response.totalResults);
    }

    public async Task<IEnumerable<InventoryDetailDTO>> GetInventoryDetails(int documentId, int itemId)
    {
        var query = builderFactory.Create()
            .Select(
                ("ia.id", nameof(InventoryDetailNSDTO.Id)),
                ("ia.quantity", nameof(InventoryDetailNSDTO.QuantityAlloted)),
                ("ia.inventorystatus", nameof(InventoryDetailNSDTO.StatusId)),
                ("is.name", nameof(InventoryDetailNSDTO.StatusName)),
                ("ia.bin", nameof(InventoryDetailNSDTO.BinId)),
                ("bin.binnumber", nameof(InventoryDetailNSDTO.BinName))
            )
            .From("inventoryassignment ia")
            .LeftJoin("bin", "bin.id = ia.bin")
            .LeftJoin("inventorystatus is", "is.id = ia.inventorystatus")
            .WithFilters(
                DataGridFilterUtilities.Equal("ia.transactionline", itemId),
                DataGridFilterUtilities.Equal("ia.transaction", documentId)
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryDetailNSDTO>(query.Query);
        return response.items.Select(ConvertInventoryDetail);
    }

    public async Task<(IEnumerable<InventoryStatusDTO>, int)> GetInventoryStatus(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(InventoryStatusDTO.Id)),
                ("name", nameof(InventoryStatusDTO.Name))
            )
            .From("inventorystatus")
            .WithFilters(DataGridFilterUtilities.Equal("isinactive", "F"))
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
        },
        Bin = nsdto.BinId != null ? new LocationBinDTO
        {
            Id = (int)nsdto.BinId,
            BinNumber = nsdto.BinName
        } : null,
        Location = new LocationDTO
        {
            Id = nsdto.LocationId,
            Name = nsdto.LocationName
        },
    });

    private InventoryDetailDTO ConvertInventoryDetail(InventoryDetailNSDTO nsdto) => nsdto.Adapt(new InventoryDetailDTO()
    {
        Status = new InventoryStatusDTO
        {
            Name = nsdto.StatusName,
            Id = nsdto.StatusId
        },
        Bin = nsdto.BinId is not null ? new LocationBinDTO
        {
            Id = (int)nsdto.BinId,
            BinNumber = nsdto.BinName
        } : null,
    });

    private class InventoryBalanceNSDTO
    {

        public int ItemId { get; set; }
        public int? BinId { get; set; }
        public string BinName { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityCommited { get; set; }
    }

    private class InventoryDetailNSDTO
    {
        public int? Id { get; set; }
        public int? BinId { get; set; }
        public string BinName { get; set; } = "DEFAULT_BIN_NAME";
        public int StatusId { get; set; }
        public string StatusName { get; set; } = "DEFAULT_STATUS_NAME";
        public decimal QuantityAlloted { get; set; }
    }
}
