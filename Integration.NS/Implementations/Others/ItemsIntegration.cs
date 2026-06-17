using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
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
                ("itemid", nameof(ItemsDTO.ItemNumber)),
                ("id", nameof(ItemsDTO.Id)),
                ("displayname", nameof(ItemsDTO.Name)),
                ("description", nameof(ItemsDTO.Description)),
                ("purchaseunit", nameof(ItemsDTO.PurchaseUnitId)),
                ("saleunit", nameof(ItemsDTO.SaleUnitId)),
                ("stockunit", nameof(ItemsDTO.StockUnitId)),
                ("BUILTIN.DF(purchaseunit)", nameof(ItemsDTO.PurchaseUnit)),
                ("BUILTIN.DF(saleunit)", nameof(ItemsDTO.SaleUnit)),
                ("BUILTIN.DF(stockunit)", nameof(ItemsDTO.StockUnit)),
                ("(SELECT SUM(quantityonhand) FROM aggregateitemlocation WHERE item = item.id)", nameof(ItemsDTO.QuantityOnHand))
            )
            .From("item")
            .WithFilters(
                DataGridFilterUtilities.Equal("itemid", id)
            )
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsDTO>(query.Query, query.Limit, query.Offset);
        return result.items.FirstOrDefault();
    }

    public async Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsByLocationDataGridAsync(DataGridIntent intent, int location)
    {
        var query = builderFactory.Create()
            .Select(
                ("i.itemid", nameof(ItemsDTO.ItemNumber)),
                ("i.id", nameof(ItemsDTO.Id)),
                ("i.displayname", nameof(ItemsDTO.Name)),
                ("i.description", nameof(ItemsDTO.Description)),
                ("i.purchaseunit", nameof(ItemsDTO.PurchaseUnitId)),
                ("i.saleunit", nameof(ItemsDTO.SaleUnitId)),
                ("i.stockunit", nameof(ItemsDTO.StockUnitId)),
                ("BUILTIN.DF(i.purchaseunit)", nameof(ItemsDTO.PurchaseUnit)),
                ("BUILTIN.DF(i.saleunit)", nameof(ItemsDTO.SaleUnit)),
                ("BUILTIN.DF(i.stockunit)", nameof(ItemsDTO.StockUnit)),
                ("ail.quantityonhand", nameof(ItemsDTO.QuantityOnHand))
            )
            .From("item i")
            .LeftJoin("aggregateitemlocation ail", on:"ail.item = i.id")
            .LeftJoin("location loc", on:"ail.location = loc.id")
            .WithFilter(DataGridFilterUtilities.Equal("loc.id", location))
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.count);
    }

    public async Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("itemid", nameof(ItemsDTO.ItemNumber)),
                ("id", nameof(ItemsDTO.Id)),
                ("displayname", nameof(ItemsDTO.Name)),
                ("description", nameof(ItemsDTO.Description)),
                ("purchaseunit", nameof(ItemsDTO.PurchaseUnitId)),
                ("saleunit", nameof(ItemsDTO.SaleUnitId)),
                ("stockunit", nameof(ItemsDTO.StockUnitId)),
                ("BUILTIN.DF(purchaseunit)", nameof(ItemsDTO.PurchaseUnit)),
                ("BUILTIN.DF(saleunit)", nameof(ItemsDTO.SaleUnit)),
                ("BUILTIN.DF(stockunit)", nameof(ItemsDTO.StockUnit)),
                ("(SELECT SUM(quantityonhand) FROM aggregateitemlocation WHERE item = item.id)", nameof(ItemsDTO.QuantityOnHand))
            )
            .From("item")
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.count);
    }
}
