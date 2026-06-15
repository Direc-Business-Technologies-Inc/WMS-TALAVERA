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
                ("BUILTIN.DF(stockunit)", nameof(ItemsDTO.StockUnit))
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
                ("itemid", nameof(ItemsDTO.ItemNumber)),
                ("id", nameof(ItemsDTO.Id)),
                ("displayname", nameof(ItemsDTO.Name)),
                ("description", nameof(ItemsDTO.Description)),
                ("purchaseunit", nameof(ItemsDTO.PurchaseUnitId)),
                ("saleunit", nameof(ItemsDTO.SaleUnitId)),
                ("stockunit", nameof(ItemsDTO.StockUnitId)),
                ("BUILTIN.DF(purchaseunit)", nameof(ItemsDTO.PurchaseUnit)),
                ("BUILTIN.DF(saleunit)", nameof(ItemsDTO.SaleUnit)),
                ("BUILTIN.DF(stockunit)", nameof(ItemsDTO.StockUnit))
            )
            .From("item")
            .WithDatagridIntent(intent)
            .Build();

        var result = await netsuiteService.ExecuteSuiteQLQuery<ItemsDTO>(query.Query, query.Limit, query.Offset);
        return (result.items, result.count);
    }
}
