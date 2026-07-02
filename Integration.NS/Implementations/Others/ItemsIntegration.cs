using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.DataTransferObjects.Others;
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

public class ItemsIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IItemsIntegration
{
    public async Task<ItemsDTO?> GetItem(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("(SELECT SUM(quantityonhand) FROM aggregateitemlocation WHERE item = item.id)", nameof(ItemsNSDTO.QuantityOnHand)),
                ("itemid", nameof(ItemsNSDTO.ItemNumber)),
                ("id", nameof(ItemsNSDTO.Id)),
                ("displayname", nameof(ItemsNSDTO.Name)),
                ("usebins", nameof(ItemsNSDTO.UseBins)),
                ("description", nameof(ItemsNSDTO.Description)),
                ("purchaseunit", nameof(ItemsNSDTO.PurchaseUnitId)),
                ("saleunit", nameof(ItemsNSDTO.SaleUnitId)),
                ("stockunit", nameof(ItemsNSDTO.StockUnitId)),
                ("BUILTIN.DF(purchaseunit)", nameof(ItemsNSDTO.PurchaseUnitName)),
                ("BUILTIN.DF(saleunit)", nameof(ItemsNSDTO.SaleUnitName)),
                ("BUILTIN.DF(stockunit)", nameof(ItemsNSDTO.StockUnitName)),
                ("u1.conversionrate", nameof(ItemsNSDTO.SaleUnitRate)),
                ("u2.conversionrate", nameof(ItemsNSDTO.StockUnitRate)),
                ("u3.conversionrate", nameof(ItemsNSDTO.PurchaseUnitRate))
            )
            .From("item")
            .LeftJoin("unitsTypeUom u1", on: "u1.internalid = i.saleunit")
            .LeftJoin("unitsTypeUom u2", on: "u2.internalid = i.stockunit")
            .LeftJoin("unitsTypeUom u3", on: "u3.internalid = i.purchaseunit")
            .WithFilters(
                DataGridFilterUtilities.Equal("itemid", id)
            )
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = result.items.FirstOrDefault();
        if (nsdto is null) return null;

        return ConvertItemNSDTO(nsdto);
    }

    public async Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsByLocationDataGridAsync(DataGridIntent intent, int location)
    {
        var query = builderFactory.Create()
            .Select(
                ("i.itemid", nameof(ItemsNSDTO.ItemNumber)),
                ("i.id", nameof(ItemsNSDTO.Id)),
                ("i.displayname", nameof(ItemsNSDTO.Name)),
                ("i.description", nameof(ItemsNSDTO.Description)),
                ("i.purchaseunit", nameof(ItemsNSDTO.PurchaseUnitId)),
                ("i.usebins", nameof(ItemsNSDTO.UseBins)),
                ("i.saleunit", nameof(ItemsNSDTO.SaleUnitId)),
                ("i.stockunit", nameof(ItemsNSDTO.StockUnitId)),
                ("BUILTIN.DF(i.purchaseunit)", nameof(ItemsNSDTO.PurchaseUnitName)),
                ("BUILTIN.DF(i.saleunit)", nameof(ItemsNSDTO.SaleUnitName)),
                ("BUILTIN.DF(i.stockunit)", nameof(ItemsNSDTO.StockUnitName)),
                ("u1.conversionrate", nameof(ItemsNSDTO.SaleUnitRate)),
                ("u2.conversionrate", nameof(ItemsNSDTO.StockUnitRate)),
                ("u3.conversionrate", nameof(ItemsNSDTO.PurchaseUnitRate)),
                ("ail.quantityonhand", nameof(ItemsDTO.QuantityOnHand))
            )
            .From("item i")
            .LeftJoin("aggregateitemlocation ail", on:"ail.item = i.id")
            .LeftJoin("location loc", on:"ail.location = loc.id")
            .LeftJoin("unitsTypeUom u1", on: "u1.internalid = i.saleunit")
            .LeftJoin("unitsTypeUom u2", on: "u2.internalid = i.stockunit")
            .LeftJoin("unitsTypeUom u3", on: "u3.internalid = i.purchaseunit")
            .WithFilter(DataGridFilterUtilities.Equal("loc.id", location))
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsNSDTO>(query.Query, query.Limit, query.Offset);
        return (result.items.Select(ConvertItemNSDTO), result.count);
    }

    public async Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("(SELECT SUM(quantityonhand) FROM aggregateitemlocation WHERE item = i.id)", nameof(ItemsNSDTO.QuantityOnHand)),
                ("i.itemid", nameof(ItemsNSDTO.ItemNumber)),
                ("i.id", nameof(ItemsNSDTO.Id)),
                ("i.displayname", nameof(ItemsNSDTO.Name)),
                ("i.description", nameof(ItemsNSDTO.Description)),
                ("i.usebins", nameof(ItemsNSDTO.UseBins)),
                ("i.purchaseunit", nameof(ItemsNSDTO.PurchaseUnitId)),
                ("i.saleunit", nameof(ItemsNSDTO.SaleUnitId)),
                ("i.stockunit", nameof(ItemsNSDTO.StockUnitId)),
                ("BUILTIN.DF(i.purchaseunit)", nameof(ItemsNSDTO.PurchaseUnitName)),
                ("BUILTIN.DF(i.saleunit)", nameof(ItemsNSDTO.SaleUnitName)),
                ("BUILTIN.DF(i.stockunit)", nameof(ItemsNSDTO.StockUnitName)),
                ("u1.conversionrate", nameof(ItemsNSDTO.SaleUnitRate)),
                ("u2.conversionrate", nameof(ItemsNSDTO.StockUnitRate)),
                ("u3.conversionrate", nameof(ItemsNSDTO.PurchaseUnitRate))
            )
            .From("item i")
            .LeftJoin("unitsTypeUom u1", on: "u1.internalid = i.saleunit")
            .LeftJoin("unitsTypeUom u2", on: "u2.internalid = i.stockunit")
            .LeftJoin("unitsTypeUom u3", on: "u3.internalid = i.purchaseunit")
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsNSDTO>(query.Query, query.Limit, query.Offset);
        return (result.items.Select(ConvertItemNSDTO), result.count);
    }

    public async Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(ItemsDTO item, DataGridIntent intent)
    {
        return await GetItemUnits(item.Id, intent);
    }

    public async Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(int itemId, DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("uom.internalid", nameof(ItemUnitDTO.Id)),
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

    private ItemsDTO ConvertItemNSDTO(ItemsNSDTO nsdto)
    {
        var dto = nsdto.Adapt<ItemsDTO>();
        dto.StockUnit = new ItemUnitDTO()
        {
            Name = nsdto.StockUnitName,
            ConversionRate = nsdto.StockUnitRate,
            Id = nsdto.StockUnitId
        };
        dto.PurchaseUnit = new ItemUnitDTO()
        {
            Name = nsdto.PurchaseUnitName,
            ConversionRate = nsdto.PurchaseUnitRate,
            Id = nsdto.PurchaseUnitId
        };
        dto.SaleUnit = new ItemUnitDTO()
        {
            Name = nsdto.PurchaseUnitName,
            ConversionRate = nsdto.PurchaseUnitRate,
            Id = nsdto.PurchaseUnitId
        };
        return dto;
    }
}
