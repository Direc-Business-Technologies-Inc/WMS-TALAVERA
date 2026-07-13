using Application.DataTransferObjects.Transactions.Packing.STR;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Database.Libraries.Repositories;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class StockTransferRequestPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    ISqlQueryManager sqlQuery,
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
                Equal("t.tosubsidiary", subsidiaryId))
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
                ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StrPackingHeaderNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .WithSubsidiaries(httpContextAccessor, "t")
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
        var mobileLineQuery = sqlQuery.ResolveSuiteQLScript(
            "NS_TO_x_Packing_Get_Items",
            new Dictionary<string, string>
            {
                ["tranid"] = id
            });

        var query = builderFactory.Create()
            .Select(
                ("q.MaterialCode", nameof(StrPackingLineNSDTO.ItemCode)),
                ("q.MaterialName", nameof(StrPackingLineNSDTO.ItemDescription)),
                ("q.UoMName", nameof(StrPackingLineNSDTO.UoM)),
                ("q.LocationName", nameof(StrPackingLineNSDTO.Warehouse)),
                ("(q.LineQuantity / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityPlanned)),
                ("(q.LineQuantityPacked / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityReceived)),
                ("(q.LineQuantityBackOrdered / q.UoMRate)", nameof(StrPackingLineNSDTO.QuantityBackOrdered))
            )
            .From($"({mobileLineQuery}) q")
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
            QuantityPlanned = nsdto.QuantityPlanned,
            QuantityReceived = nsdto.QuantityReceived,
            QuantityBackOrdered = nsdto.QuantityBackOrdered,
        };
    }
}
