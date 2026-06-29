using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
using Shared.Entities;
using Shared.Libraries.Utilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class StockTransferRequestPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IStockTransferRequestPackingIntegration
{
    public async Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> GetPackingStockTransferRequestList(DataGridIntent intent)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingDataGridNSDTO.Date)),
                    ("t.tranid", nameof(StrPackingDataGridNSDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StrPackingDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StrPackingDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StrPackingDataGridNSDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StrPackingDataGridNSDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StrPackingDataGridNSDTO.SourceLocation)),
                    ("s.name", nameof(StrPackingDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StrPackingDataGridNSDTO.StatusId)),
                    ("t.memo", nameof(StrPackingDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("transferorderstatus s", on: "s.id = t.status")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" }),
                    DataGridFilterUtilities.In("t.status", new string[] { "B", "D" }),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 4)
                )
                .WithDatagridIntent(intent)
                .Build();

        var response = await query.ExecuteWithPaging<StrPackingDataGridNSDTO>(netsuiteService);
        return (response.items.Select(ConvertDataGridDTO), response.totalResults);
    }

    public async Task<StockTransferRequestInfoPackingDTO?> GetPackingStockTransferRequest(string id)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingHeaderNSDTO.Date)),
                    ("t.tranid", nameof(StrPackingHeaderNSDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StrPackingHeaderNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.custbody_dbti_return_to_vendor)", nameof(StrPackingHeaderNSDTO.VendorName)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StrPackingHeaderNSDTO.SubsidiaryName)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StrPackingHeaderNSDTO.ToSubsidiaryName)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StrPackingHeaderNSDTO.DestinationLocationName)),
                    ("BUILTIN.DF(tl.location)", nameof(StrPackingHeaderNSDTO.SourceLocationName)),
                    ("t.custbody_dbti_return_to_vendor", nameof(StrPackingHeaderNSDTO.VendorId)),
                    ("t.subsidiary", nameof(StrPackingHeaderNSDTO.SubsidiaryId)),
                    ("t.tosubsidiary", nameof(StrPackingHeaderNSDTO.ToSubsidiaryId)),
                    ("t.transferlocation", nameof(StrPackingHeaderNSDTO.DestinationLocationId)),
                    ("tl.location", nameof(StrPackingHeaderNSDTO.SourceLocationId)),
                    ("t.memo", nameof(StrPackingHeaderNSDTO.Remarks)),
                    ("t.custbody_dbti_transfer_category", nameof(StrPackingHeaderNSDTO.TransferCategoryId)),
                    ("s.name", nameof(StrPackingHeaderNSDTO.StatusName)),
                    ("s.id", nameof(StrPackingHeaderNSDTO.StatusId)),
                    ("BUILTIN.DF(t.custbody_dbti_transfer_category)", nameof(StrPackingHeaderNSDTO.TransferCategoryName))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("transferorderstatus s", on: "s.id = t.status")
                .WithFilters(
                    DataGridFilterUtilities.In("t.status", new string[] { "B", "D" }),
                    DataGridFilterUtilities.Equal("t.tranid", id),
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" }),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 4)
                )
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StrPackingHeaderNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = response.items.FirstOrDefault();
        if (nsdto is null) return null;

        var dto = nsdto.Adapt<StockTransferRequestInfoPackingDTO>();

        dto.Vendor = new() { Name = nsdto.VendorName, Id = nsdto.VendorId };
        dto.SourceLocation = new() { Name = nsdto.SourceLocationName, Id = nsdto.SourceLocationId };
        dto.SourceLocation = new() { Name = nsdto.SourceLocationName, Id = nsdto.SourceLocationId };
        dto.DestinationLocation = new() { Name = nsdto.DestinationLocationName, Id = nsdto.DestinationLocationId };
        dto.Subsidiary = new() { Name = nsdto.SubsidiaryName, Id = nsdto.SubsidiaryId };
        dto.ToSubsidiary = new() { Name = nsdto.ToSubsidiaryName, Id = nsdto.ToSubsidiaryId };
        dto.Status = new() { Name = nsdto.StatusName, Id = nsdto.StatusId };
        dto.TransferCategory = nsdto.TransferCategoryId switch
        {
            1 => TransferCategoryPacking.Transfer,
            2 => TransferCategoryPacking.IntercompanyTransfer,
            3 => TransferCategoryPacking.ReturnsGood,
            4 => TransferCategoryPacking.ReturnsBad,
            _ => throw new NotImplementedException($"Current WMS version does not support transfer category: {nsdto.TransferCategoryName}")
        };

        return dto;
    }

    public async Task<IEnumerable<StockTransferRequestLinePackingDTO>?> GetPackingStockTransferRequestLines(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.id", nameof(StrPackingLineNSDTO.ItemId)),
                ("item.itemid", nameof(StrPackingLineNSDTO.ItemCode)),
                ("uom.unitName", nameof(StrPackingLineNSDTO.UoMName)),
                ("uom.internalid", nameof(StrPackingLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(StrPackingLineNSDTO.UoMRate)),
                ("BUILTIN.DF(tl.location)", nameof(StrPackingLineNSDTO.Warehouse)),
                ("item.displayname", nameof(StrPackingLineNSDTO.ItemDescription)),
                ("(iil.quantityonhand / uom.conversionrate)", nameof(StrPackingLineNSDTO.QuantityOnHand)),
                ("(tl.quantity / uom.conversionrate)", nameof(StrPackingLineNSDTO.QuantityAlloted))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .Join("unitsTypeUom uom", on: "tl.units = uom.internalid")
            .Join("inventoryitemlocations iil", on: "tl.item = iil.item AND tl.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("tl.transactionlinetype", "SHIPPING"),
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StrPackingLineNSDTO>(query.Query, query.Limit, query.Offset);
        return [.. response.items.Select(x => x.Adapt(new StockTransferRequestLinePackingDTO {
            UoM = new Application.DataTransferObjects.Others.ItemUnitDTO{
                ConversionRate = x.UoMRate,
                Name = x.UoMName,
                Id = x.UoMId
            }
        }))];
    }

    public async Task<(IEnumerable<TransferOrderStatusPacking> data, int count)> GetPackingTransferOrderStatuses(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(TransferOrderStatusPacking.Id)),
                ("name", nameof(TransferOrderStatusPacking.Name))
            )
            .From("transferorderstatus")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<TransferOrderStatusPacking>(netsuiteService);

        return (response.items, response.totalResults);
    }

    private static StockTransferRequestPackingDataGridDTO ConvertDataGridDTO(StrPackingDataGridNSDTO nsdto)
    {
        var dto = nsdto.Adapt<StockTransferRequestPackingDataGridDTO>();
        dto.Status = new TransferOrderStatusPacking
        {
            Id = nsdto.StatusId,
            Name = nsdto.StatusName
        };
        return dto;
    }
}
