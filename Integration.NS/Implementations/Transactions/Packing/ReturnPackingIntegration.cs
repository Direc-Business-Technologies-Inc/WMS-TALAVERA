using Application.DataTransferObjects.Transactions.Packing;
using Application.DataTransferObjects.Transactions.Packing.Returns;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Database.Libraries.Repositories;
using Integration.NS.DataTransferObjects.Packing.Returns;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using static Shared.Libraries.Utilities.DataGridFilterUtilities;

namespace Integration.NS.Implementations.Transactions.Packing;

internal class ReturnPackingIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    ISqlQueryManager sqlQuery,
    SuiteQLQueryBuilderFactoryService builderFactory) : IReturnPackingIntegration
{
    public async Task<(IEnumerable<ReturnsDataGridDTO> Data, int Count)> GetPackingReturnsList(DataGridIntent intent, int subsidiaryId)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(ReturnPackingDataGridNSDTO.Id)),
                ("t.tranid", nameof(ReturnPackingDataGridNSDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(ReturnPackingDataGridNSDTO.Date)),
                ("BUILTIN.DF(t.subsidiary)", nameof(ReturnPackingDataGridNSDTO.SourceSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(ReturnPackingDataGridNSDTO.DestinationSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(ReturnPackingDataGridNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(ReturnPackingDataGridNSDTO.TransferLocation)),
                ("s.name", nameof(ReturnPackingDataGridNSDTO.Status)),
                ("t.memo", nameof(ReturnPackingDataGridNSDTO.Remarks))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("transferorderstatus s", on: "s.id = t.status")
            .WithFilters(
                Equal("tl.mainline", "T"),
                Equal("t.tosubsidiary", subsidiaryId))
            .WithFilters(PackingReturnsFilters())
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ReturnPackingDataGridNSDTO>(netsuiteService);

        return (response.items.Select(MapDataGridDto), response.totalResults);
    }

    public async Task<ReturnsInfoDTO?> GetPackingReturn(string id)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(ReturnPackingHeaderNSDTO.Id)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(ReturnPackingHeaderNSDTO.Date)),
                ("t.tranid", nameof(ReturnPackingHeaderNSDTO.ReferenceNumber)),
                ("BUILTIN.DF(t.subsidiary)", nameof(ReturnPackingHeaderNSDTO.FromSubsidiary)),
                ("BUILTIN.DF(t.tosubsidiary)", nameof(ReturnPackingHeaderNSDTO.ToSubsidiary)),
                ("BUILTIN.DF(tl.location)", nameof(ReturnPackingHeaderNSDTO.Location)),
                ("BUILTIN.DF(t.transferlocation)", nameof(ReturnPackingHeaderNSDTO.TransferLocation)),
                ("BUILTIN.DF(t.custbody_dbti_transfer_category)", nameof(ReturnPackingHeaderNSDTO.TransferCategory)),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(ReturnPackingHeaderNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", on: "tl.transaction = t.id")
            .LeftJoin("employee e", "t.custbody_dbti_prepared_by = e.id")
            .WithFilters(
                Equal("t.tranid", id),
                Equal("tl.mainline", "T"))
            .WithFilters(PackingReturnsFilters())
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<ReturnPackingHeaderNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = response.items.FirstOrDefault();

        return nsdto is null ? null : MapInfoDto(nsdto);
    }

    public async Task<(IEnumerable<ReturnsLineDTO> Data, int Count)> GetPackingReturnLines(string id, DataGridIntent intent)
    {
        var mobileLineQuery = sqlQuery.ResolveSuiteQLScript(
            "NS_TO_x_Return_x_Packing_Get_Items",
            new Dictionary<string, string>
            {
                ["tranid"] = id
            });

        var query = builderFactory.Create()
            .Select(
                ("q.MaterialCode", nameof(ReturnPackingLineNSDTO.ItemCode)),
                ("q.MaterialName", nameof(ReturnPackingLineNSDTO.ItemDescription)),
                ("q.UoMName", nameof(ReturnPackingLineNSDTO.UoM)),
                ("q.LocationName", nameof(ReturnPackingLineNSDTO.Warehouse)),
                ("q.LineQuantity", nameof(ReturnPackingLineNSDTO.QuantityPlanned)),
                ("q.LineQuantityPacked", nameof(ReturnPackingLineNSDTO.QuantityReceived)),
                ("q.LineQuantityBackOrdered", nameof(ReturnPackingLineNSDTO.QuantityBackOrdered))
            )
            .From($"({mobileLineQuery}) q")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ReturnPackingLineNSDTO>(netsuiteService);

        return (response.items.Select(MapLineDto), response.totalResults);
    }

    private static AppFilterDescriptor[] PackingReturnsFilters()
    {
        return
        [
            In("t.recordtype", new string[] { "intercompanytransferorder" }),
            In("t.custbody_dbti_transfer_category", new string[] { "3", "4" }),
            Equal("t.ordpicked", "F"),
            In("t.status", new string[] { "B", "D", "E" })
        ];
    }

    private static ReturnsDataGridDTO MapDataGridDto(ReturnPackingDataGridNSDTO nsdto)
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

    private static ReturnsInfoDTO MapInfoDto(ReturnPackingHeaderNSDTO nsdto)
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
            TransferCategory = nsdto.TransferCategory,
            PreparedBy = nsdto.PreparedBy,
        };
    }

    private static ReturnsLineDTO MapLineDto(ReturnPackingLineNSDTO nsdto)
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

    public async Task<(IEnumerable<PackedItemFulfillmentDTO> Data, int Count)> GetPackedItemFulfillments(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(PackedItemFulfillmentDTO.Id)),
                ("t.tranid", nameof(PackedItemFulfillmentDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(PackedItemFulfillmentDTO.Date)),
                ("TO_CHAR(t.LastModifiedDate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(PackedItemFulfillmentDTO.DateLastModified)),
                ("BUILTIN.DF(t.custbody_dbti_transfer_category)", nameof(PackedItemFulfillmentDTO.TransferCategory)),
                ("BUILTIN.DF(tl.createdfrom)", nameof(PackedItemFulfillmentDTO.CreatedFrom))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.mainline = 'T' and tl.transaction = t.id")
            .WithFilters(
                Equal("t.recordtype", "itemfulfillment"),
                Equal("t.status", "B")
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<PackedItemFulfillmentDTO>(netsuiteService);

        return (response.items, response.totalResults);
    }
}
