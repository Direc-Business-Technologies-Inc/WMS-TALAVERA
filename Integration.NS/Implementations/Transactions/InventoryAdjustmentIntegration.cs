using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Integration.NS.DataTransferObjects;
using Integration.NS.Services;
using Mapster;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class InventoryAdjustmentIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IInventoryAdjustmentIntegration
{
    public async Task<InventoryAdjustmentDTO?> GetInventoryAdjustmentAsync(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryAdjustmentNSDTO.Id)),
                ("t.tranid", nameof(InventoryAdjustmentNSDTO.ReferenceNumber)),
                ("t.memo", nameof(InventoryAdjustmentNSDTO.Memo)),
                ("t.custbody_dbti_prepared_by", nameof(InventoryAdjustmentNSDTO.PreparedBy)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentNSDTO.Location)),
                ("tl.location", nameof(InventoryAdjustmentNSDTO.LocationId)),
                ("BUILTIN.DF(t.account)", nameof(InventoryAdjustmentNSDTO.Account)),
                ("t.account", nameof(InventoryAdjustmentNSDTO.AccountId)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryAdjustmentNSDTO.Subsidiary)),
                ("t.subsidiary", nameof(InventoryAdjustmentNSDTO.SubsidiaryId))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "inventoryadjustment"),
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "T")
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentNSDTO>(query.Query, query.Limit, query.Offset);

        var nsdto = response.items.FirstOrDefault();
        if (nsdto is null) return null;

        var result = nsdto.Adapt<InventoryAdjustmentDTO>();

        result.Subsidiary = new SubsidiaryDTO { Id = nsdto.SubsidiaryId, Name = nsdto.Subsidiary };
        result.Location = new LocationDTO { Id = nsdto.LocationId, Name = nsdto.Location };
        result.Account = new BusinessAccountDTO { Id = nsdto.AccountId, Name = nsdto.Account };

        return result;
    }

    public async Task<IEnumerable<InventoryAdjustmentLineDTO>> GetInventoryAdjustmentLinesAsync(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemId", nameof(InventoryAdjustmentLineDTO.ItemCode)),
                ("item.displayname", nameof(InventoryAdjustmentLineDTO.ItemDescription)),
                ("BUILTIN.DF(tl.units)", nameof(InventoryAdjustmentLineDTO.UoM)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentLineDTO.Location)),
                ("tl.quantity", nameof(InventoryAdjustmentLineDTO.QuantityOnHand))
            )
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentLineDTO>(query.Query);
        return response.items;
    }

    public async Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetInventoryAdjustmentsAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryAdjustmentDataGridDTO.Id)),
                ("t.tranid", nameof(InventoryAdjustmentDataGridDTO.ReferenceNumber)),
                ("t.memo", nameof(InventoryAdjustmentDataGridDTO.Memo)),
                ("t.custbody_dbti_prepared_by", nameof(InventoryAdjustmentDataGridDTO.PreparedBy)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentDataGridDTO.Location)),
                ("BUILTIN.DF(t.account)", nameof(InventoryAdjustmentDataGridDTO.Account)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryAdjustmentDataGridDTO.Subsidiary))
            )
            .From("transaction t")
            .Join("transactionline tl", on:"tl.transaction = t.id")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "inventoryadjustment"),
                DataGridFilterUtilities.Equal("tl.mainline", "T")
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentDataGridDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }
}
