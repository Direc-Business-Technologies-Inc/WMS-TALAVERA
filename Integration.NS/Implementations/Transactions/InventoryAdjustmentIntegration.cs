using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Integration.NS.DataTransferObjects.InventoryAdjustment;
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
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentNSDTO.LocationName)),
                ("tl.location", nameof(InventoryAdjustmentNSDTO.LocationId)),
                ("BUILTIN.DF(t.account)", nameof(InventoryAdjustmentNSDTO.AccountName)),
                ("t.account", nameof(InventoryAdjustmentNSDTO.AccountId)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryAdjustmentNSDTO.SubsidiaryName)),
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

        result.Subsidiary = new SubsidiaryDTO { Id = nsdto.SubsidiaryId, Name = nsdto.SubsidiaryName };
        result.Location = new LocationDTO { Id = nsdto.LocationId, Name = nsdto.LocationName };
        result.Account = new BusinessAccountDTO { Id = nsdto.AccountId, Name = nsdto.AccountName };

        return result;
    }

    public async Task<IEnumerable<InventoryAdjustmentLineDTO>> GetInventoryAdjustmentLinesAsync(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemId", nameof(InventoryAdjustmentLineNSDTO.ItemCode)),
                ("item.displayname", nameof(InventoryAdjustmentLineNSDTO.ItemDescription)),
                ("BUILTIN.DF(tl.units)", nameof(InventoryAdjustmentLineNSDTO.UoMName)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryAdjustmentLineNSDTO.LocationName)),
                ("tl.units", nameof(InventoryAdjustmentLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(InventoryAdjustmentLineNSDTO.UoMRate)),
                ("tl.location", nameof(InventoryAdjustmentLineNSDTO.LocationId)),
                ("iil.quantityonhand", nameof(InventoryAdjustmentLineNSDTO.QuantityOnHand))
            )
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .LeftJoin("unitstypeuom uom", on: "tl.units = uom.internalid")
            .LeftJoin("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryAdjustmentLineNSDTO>(query.Query);
        return response.items.Select(ConvertLine);
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

    private InventoryAdjustmentLineDTO ConvertLine(InventoryAdjustmentLineNSDTO nsdto)
    {
        var dto = nsdto.Adapt<InventoryAdjustmentLineDTO>();

        dto.Location = new LocationDTO
        {
            Name = nsdto.LocationName,
            Id = nsdto.LocationId
        };
        dto.UoM = new ItemUnitDTO
        {
            Name = nsdto.UoMName,
            Id = nsdto.UoMId,
            ConversionRate = nsdto.UoMRate
        };
        return dto;
    }
}
