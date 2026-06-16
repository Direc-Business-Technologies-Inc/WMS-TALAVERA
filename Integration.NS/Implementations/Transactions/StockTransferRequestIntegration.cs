using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Integration.NS.DataTransferObjects;
using Integration.NS.Helpers;
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

internal class StockTransferRequestIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory) : IStockTransferRequestIntegration
{
    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetIntercompanyTransferOrderList(DataGridIntent intent)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridDTO.Date)),
                    ("t.tranid", nameof(StockTransferRequestDataGridDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StockTransferRequestDataGridDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridDTO.SourceLocation))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .WithFilters(
                    DataGridFilterUtilities.Equal("t.recordtype", "intercompanytransferorder"),
                    DataGridFilterUtilities.Equal("t.status", "A"),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 4)
                )
                .WithDatagridIntent(intent)
                .Build();

        var response = await query.ExecuteWithPaging<StockTransferRequestDataGridDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetReturnsList(DataGridIntent intent)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridDTO.Date)),
                    ("t.tranid", nameof(StockTransferRequestDataGridDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StockTransferRequestDataGridDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridDTO.SourceLocation))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .WithFilters(
                    DataGridFilterUtilities.In("t.recordtype", new string[] {"intercompanytransferorder", "transferorder" }),
                    DataGridFilterUtilities.Equal("t.status", "A"),
                    DataGridFilterUtilities.Any(
                        DataGridFilterUtilities.Equal("t.custbody_dbti_transfer_category", 3),
                        DataGridFilterUtilities.Equal("t.custbody_dbti_transfer_category", 4))
                )
                .WithDatagridIntent(intent)
                .Build();
        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetTransferOrderList(DataGridIntent intent)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridDTO.Date)),
                    ("t.tranid", nameof(StockTransferRequestDataGridDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StockTransferRequestDataGridDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridDTO.SourceLocation))
                )
                .From("transaction t")
                .Join("transactionline tl", on:"tl.transaction = t.id")
                .WithFilters(
                    DataGridFilterUtilities.Equal("t.recordtype", "transferorder"),
                    DataGridFilterUtilities.Equal("t.status", "A"),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 4)
                )
                .WithDatagridIntent(intent)
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridDTO>(query.Query, query.Limit, query.Offset);

        return (response.items, response.totalResults);
    }



    public async Task<StockTransferRequestInfoDTO?> GetStockTransferRequest(string id)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestHeaderNSDTO.Date)),
                    ("t.tranid", nameof(StockTransferRequestHeaderNSDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(StockTransferRequestHeaderNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.custbody_dbti_return_to_vendor)", nameof(StockTransferRequestHeaderNSDTO.VendorName)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestHeaderNSDTO.SubsidiaryName)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestHeaderNSDTO.ToSubsidiaryName)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestHeaderNSDTO.DestinationLocationName)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestHeaderNSDTO.SourceLocationName)),
                    ("t.custbody_dbti_return_to_vendor", nameof(StockTransferRequestHeaderNSDTO.VendorId)),
                    ("t.subsidiary", nameof(StockTransferRequestHeaderNSDTO.SubsidiaryId)),
                    ("t.tosubsidiary", nameof(StockTransferRequestHeaderNSDTO.ToSubsidiaryId)),
                    ("t.transferlocation", nameof(StockTransferRequestHeaderNSDTO.DestinationLocationId)),
                    ("tl.location", nameof(StockTransferRequestHeaderNSDTO.SourceLocationId)),
                    ("CASE " + 
                        "WHEN t.custbody_dbti_transfer_category IN (3, 4) THEN 'Returns' " +
                        "WHEN t.recordtype = 'intercompanytransferorder' THEN 'IntercompanyTransferOrder' " + 
                        "ELSE 'TransferOrder' END",
                        nameof(StockTransferRequestInfoDTO.Type))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .WithFilters(
                    DataGridFilterUtilities.Equal("t.tranid", id),
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.In("t.recordtype", new string[] { "transferorder", "intercompanytransferorder" })
                )
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestHeaderNSDTO>(query.Query, query.Limit, query.Offset);
        var nsdto = response.items.FirstOrDefault();
        if (nsdto is null) return null;

        var dto = nsdto.Adapt<StockTransferRequestInfoDTO>();

        dto.Vendor = new() { Name = nsdto.VendorName, Id = nsdto.VendorId };
        dto.SourceLocation = new() { Name = nsdto.SourceLocationName, Id = nsdto.SourceLocationId };
        dto.SourceLocation = new() { Name = nsdto.SourceLocationName, Id = nsdto.SourceLocationId };
        dto.DestinationLocation = new() { Name = nsdto.DestinationLocationName, Id = nsdto.DestinationLocationId };
        dto.Subsidiary = new() { Name = nsdto.SubsidiaryName, Id = nsdto.SubsidiaryId };
        dto.ToSubsidiary = new() { Name = nsdto.ToSubsidiaryName, Id = nsdto.ToSubsidiaryId };

        return dto;
    }

    public async Task<IEnumerable<StockTransferRequestLineDTO>?> GetStockTransferRequestLines(string id)
    {

        var query = builderFactory.Create()
            .Select(
                ("item.itemid", nameof(StockTransferRequestLineDTO.ItemCode)),
                ("BUILTIN.DF(tl.units)", nameof(StockTransferRequestLineDTO.UoM)),
                ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestLineDTO.Warehouse)),
                ("item.displayname", nameof(StockTransferRequestLineDTO.ItemDescription)),
                ("tl.quantity", nameof(StockTransferRequestLineDTO.QuantityOnHand))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestLineDTO>(query.Query, query.Limit, query.Offset);
        return [..response.items];
    }
}
