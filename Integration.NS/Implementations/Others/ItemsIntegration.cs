using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.DataTransferObjects.Others;
using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Others;

public class ItemsIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IItemsIntegration
{
    public async Task<ItemsDTO?> GetItem(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("itemid", nameof(ItemsNSDTO.ItemNumber)),
                ("id", nameof(ItemsNSDTO.Id)),
                ("displayname", nameof(ItemsNSDTO.Name)),
                ("description", nameof(ItemsNSDTO.Description)),
                ("purchaseunit", nameof(ItemsNSDTO.PurchaseUnitId)),
                ("saleunit", nameof(ItemsNSDTO.SaleUnitId)),
                ("stockunit", nameof(ItemsNSDTO.StockUnitId)),
                ("BUILTIN.DF(purchaseunit)", nameof(ItemsNSDTO.PurchaseUnit)),
                ("BUILTIN.DF(saleunit)", nameof(ItemsNSDTO.SaleUnit)),
                ("BUILTIN.DF(stockunit)", nameof(ItemsNSDTO.StockUnit))
            )
            .From("item")
            .WithFilters(
                DataGridFilterUtilities.Equal("itemid", id)
            )
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsDTO>(query.Query, query.Limit, query.Offset);
        return result.items.FirstOrDefault();
    }

    public async Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("itemid", nameof(ItemsNSDTO.ItemNumber)),
                ("id", nameof(ItemsNSDTO.Id)),
                ("displayname", nameof(ItemsNSDTO.Name)),
                ("description", nameof(ItemsNSDTO.Description)),
                ("purchaseunit", nameof(ItemsNSDTO.PurchaseUnitId)),
                ("saleunit", nameof(ItemsNSDTO.SaleUnitId)),
                ("stockunit", nameof(ItemsNSDTO.StockUnitId)),
                ("BUILTIN.DF(purchaseunit)", nameof(ItemsNSDTO.PurchaseUnit)),
                ("BUILTIN.DF(saleunit)", nameof(ItemsNSDTO.SaleUnit)),
                ("BUILTIN.DF(stockunit)", nameof(ItemsNSDTO.StockUnit))
            )
            .From("item")
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.count);
    }

    public async Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(ItemsDTO item, DataGridIntent intent)
    {
        return await GetItemUnits(item.Id, intent);
    }

    public async Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(int itemId, DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("uom.id", nameof(ItemUnitDTO.Id)),
                ("uom.unitname", nameof(ItemUnitDTO.Name)),
                ("uom.abbreviation", nameof(ItemUnitDTO.Abbreviation)),
                ("uom.conversionrate", nameof(ItemUnitDTO.ConversionRate))
            )
            .From("unitsTypeUom uom")
            .Join("item i", "i.unitstype = uom.unitstype")
            .WithDatagridIntent(intent)
            .WithFilter(DataGridFilterUtilities.Equal("i.id", itemId))
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemUnitDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.count);
    }
}
