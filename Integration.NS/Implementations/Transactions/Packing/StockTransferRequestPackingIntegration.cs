using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Shared.Entities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class StockTransferRequestPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IStockTransferRequestPackingIntegration
{
    public async Task<(IEnumerable<StockTransferRequestPackingDataGridDTO> Data, int Count)> GetPackingStockTransferRequestList(DataGridIntent intent, int subsidiaryId)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(StrPackingDataGridNSDTO.Id)),
                ("t.tranid", nameof(StrPackingDataGridNSDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingDataGridNSDTO.Date)),
                ("BUILTIN.DF(t.subsidiary)", nameof(StrPackingDataGridNSDTO.SourceSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(StrPackingDataGridNSDTO.DestinationSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(StrPackingDataGridNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(StrPackingDataGridNSDTO.TransferLocation)),
                ("s.name", nameof(StrPackingDataGridNSDTO.Status)),
                ("t.memo", nameof(StrPackingDataGridNSDTO.Remarks))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("transferorderstatus s", on: "s.id = t.status")
            .WithFilters(
                Equal("tl.mainline", "T"),
                Equal("t.subsidiary", subsidiaryId))
            .WithFilters(PackingStockTransferRequestFilters())
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<StrPackingDataGridNSDTO>(netsuiteService);

        return (response.items.Select(MapDataGridDto), response.totalResults);
    }

    public async Task<StockTransferRequestInfoPackingDTO?> GetPackingStockTransferRequest(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(StrPackingHeaderNSDTO.Id)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StrPackingHeaderNSDTO.Date)),
                ("t.tranid", nameof(StrPackingHeaderNSDTO.ReferenceNumber)),
                ("BUILTIN.DF(t.subsidiary)", nameof(StrPackingHeaderNSDTO.FromSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(StrPackingHeaderNSDTO.ToSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(StrPackingHeaderNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(StrPackingHeaderNSDTO.TransferLocation)),
                ("t.custbody_dbti_prepared_by", nameof(StrPackingHeaderNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .WithFilters(
                Equal("t.tranid", id),
                Equal("tl.mainline", "T"))
            .WithFilters(PackingStockTransferRequestFilters())
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StrPackingHeaderNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = response.items.FirstOrDefault();

        return nsdto is null ? null : MapInfoDto(nsdto);
    }

    public async Task<(IEnumerable<StockTransferRequestLinePackingDTO> Data, int Count)> GetPackingStockTransferRequestLines(string id, DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemid", nameof(StrPackingLineNSDTO.ItemCode)),
                ("item.displayname", nameof(StrPackingLineNSDTO.ItemDescription)),
                ("BUILTIN.DF(tl.units)", nameof(StrPackingLineNSDTO.UoM)),
                ("BUILTIN.DF(tl.location)", nameof(StrPackingLineNSDTO.Warehouse)),
                ("tl.quantity", nameof(StrPackingLineNSDTO.QuantityPlanned))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .WithFilters(
                Equal("t.tranid", id),
                Equal("tl.transactionlinetype", "SHIPPING"),
                Equal("tl.mainline", "F"))
            .WithFilters(PackingStockTransferRequestFilters())
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<StrPackingLineNSDTO>(netsuiteService);

        return (response.items.Select(MapLineDto), response.totalResults);
    }

    private static AppFilterDescriptor[] PackingStockTransferRequestFilters()
    {
        return
        [
            In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
            In("t.custbody_dbti_transfer_category", new string[] { "1", "2" }),
            Equal("t.ordpicked", "F"),
            In("t.status", new string[] { "B", "D", "E" })
        ];
    }

    private static StockTransferRequestPackingDataGridDTO MapDataGridDto(StrPackingDataGridNSDTO nsdto)
    {
        return new()
        {
            Id = nsdto.Id,
            ReferenceNumber = nsdto.ReferenceNumber,
            Date = nsdto.Date,
            SourceSubsidiary = nsdto.SourceSubsidiary,
            DestinationSubsidiary = nsdto.DestinationSubsidiary,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            Status = nsdto.Status,
            Remarks = nsdto.Remarks
        };
    }

    private static StockTransferRequestInfoPackingDTO MapInfoDto(StrPackingHeaderNSDTO nsdto)
    {
        return new()
        {
            Id = nsdto.Id,
            Date = nsdto.Date,
            ReferenceNumber = nsdto.ReferenceNumber,
            FromSubsidiary = nsdto.FromSubsidiary,
            ToSubsidiary = nsdto.ToSubsidiary,
            Location = nsdto.Location,
            TransferLocation = nsdto.TransferLocation,
            PreparedBy = nsdto.PreparedBy,
            ReceivedBy = nsdto.ReceivedBy
        };
    }

    private static StockTransferRequestLinePackingDTO MapLineDto(StrPackingLineNSDTO nsdto)
    {
        return new()
        {
            ItemCode = nsdto.ItemCode,
            ItemDescription = nsdto.ItemDescription,
            UoM = nsdto.UoM,
            Warehouse = nsdto.Warehouse,
            QuantityPlanned = nsdto.QuantityPlanned
        };
    }
}
