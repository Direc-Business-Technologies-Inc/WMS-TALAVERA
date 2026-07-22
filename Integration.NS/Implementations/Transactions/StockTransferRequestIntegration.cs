using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Integration.NS.DataTransferObjects.StockTransferRequest;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

internal class StockTransferRequestIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory) : IStockTransferRequestIntegration
{
    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetIntercompanyTransferOrderList(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.DateLastModified)));
        
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.DateLastModified)),
                    ("t.tranid", nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridNSDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridNSDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridNSDTO.SourceLocation)),
                    ("s.name", nameof(StockTransferRequestDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestDataGridNSDTO.StatusId)),
                    ("t.memo", nameof(StockTransferRequestDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.Equal("t.recordtype", "intercompanytransferorder"),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 4)
                )
                .WithSubsidiaries(httpContextAccessor, "t")
                .WithDatagridIntent(intent)
                .Build();

        var response = await query.ExecuteWithPaging<StockTransferRequestDataGridNSDTO>(netsuiteService);
        return (response.items.Select(ConvertDataGridDTO), response.totalResults);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetReturnsList(DataGridIntent intent)
    {

        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.DateLastModified)));
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.DateLastModified)),
                    ("t.tranid", nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridNSDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridNSDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridNSDTO.SourceLocation)),
                    ("s.name", nameof(StockTransferRequestDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestDataGridNSDTO.StatusId)),
                    ("t.memo", nameof(StockTransferRequestDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .WithSubsidiaries(httpContextAccessor, "t")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
                    DataGridFilterUtilities.Any(
                        DataGridFilterUtilities.Equal("t.custbody_dbti_transfer_category", 3),
                        DataGridFilterUtilities.Equal("t.custbody_dbti_transfer_category", 4))
                )
                .WithDatagridIntent(intent)
                .Build();
        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridNSDTO>(query.Query, query.Limit, query.Offset);
        return (response.items.Select(ConvertDataGridDTO), response.totalResults);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetTransferOrderList(DataGridIntent intent)
    {

        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.DateLastModified)));

        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.DateLastModified)),
                    ("t.tranid", nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridNSDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridNSDTO.DestinationLocation)),
                    ("s.name", nameof(StockTransferRequestDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestDataGridNSDTO.StatusId)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridNSDTO.SourceLocation)),
                    ("t.memo", nameof(StockTransferRequestDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.Equal("t.recordtype", "transferorder"),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.NotEqual("t.custbody_dbti_transfer_category", 4)
                )
                .WithSubsidiaries(httpContextAccessor, "t")
                .WithDatagridIntent(intent)
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridNSDTO>(query.Query, query.Limit, query.Offset);
        return (response.items.Select(ConvertDataGridDTO), response.totalResults);
    }



    public async Task<StockTransferRequestInfoDTO?> GetStockTransferRequest(string id)
    {
        var query = builderFactory.Create()
                .Select(
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestHeaderNSDTO.Date)),
                    ("t.tranid", nameof(StockTransferRequestHeaderNSDTO.ReferenceNumber)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestHeaderNSDTO.PreparedBy)),
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
                    ("t.memo", nameof(StockTransferRequestHeaderNSDTO.Remarks)),
                    ("t.custbody_dbti_transfer_category", nameof(StockTransferRequestHeaderNSDTO.TransferCategoryId)),
                    ("s.name", nameof(StockTransferRequestHeaderNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestHeaderNSDTO.StatusId)),
                    ("t.id", nameof(StockTransferRequestHeaderNSDTO.Id)),
                    ("BUILTIN.DF(t.custbody_dbti_transfer_category)", nameof(StockTransferRequestHeaderNSDTO.TransferCategoryName))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .WithSubsidiaries(httpContextAccessor, "t")
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
        dto.Status = new() { Name = nsdto.StatusName, Id = nsdto.StatusId };
        dto.TransferCategory = nsdto.TransferCategoryId switch
        {
            1 => TransferCategory.Transfer,
            2 => TransferCategory.IntercompanyTransfer,
            3 => TransferCategory.ReturnsGood,
            4 => TransferCategory.ReturnsBad,
            _ => throw new NotImplementedException($"Current WMS version does not support transfer category: {nsdto.TransferCategoryName}")
        };

        return dto;
    }

    public async Task<IEnumerable<StockTransferRequestLineDTO>?> GetStockTransferRequestLines(string id)
    {

        var query = builderFactory.Create()
            .Select(
                ("item.id", nameof(StockTransferRequestLineNSDTO.ItemId)),
                ("item.itemid", nameof(StockTransferRequestLineNSDTO.ItemCode)),
                ("uom.unitName", nameof(StockTransferRequestLineNSDTO.UoMName)),
                ("uom.internalid", nameof(StockTransferRequestLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(StockTransferRequestLineNSDTO.UoMRate)),
                ("BUILTIN.DF(ml.location)", nameof(StockTransferRequestLineNSDTO.Warehouse)),
                ("item.displayname", nameof(StockTransferRequestLineNSDTO.ItemDescription)),
                ("tl.linesequencenumber", nameof(StockTransferRequestLineNSDTO.LineNumber)),
                ("(iil.quantityavailable / uom.conversionrate)", nameof(StockTransferRequestLineNSDTO.QuantityOnHand)),
                ("(-tl.quantity / uom.conversionrate)", nameof(StockTransferRequestLineNSDTO.QuantityAlloted)) // idk why this is negative
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .Join("unitsTypeUom uom", on: "tl.units = uom.internalid")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .Join("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("tl.transactionlinetype", "ITEM"),
                DataGridFilterUtilities.In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
                DataGridFilterUtilities.Equal("t.tranid", id),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            ).Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestLineNSDTO>(query.Query, query.Limit, query.Offset);
        return [.. response.items.Select(x => x.Adapt(new StockTransferRequestLineDTO {
            UoM = new Application.DataTransferObjects.Others.ItemUnitDTO{
                ConversionRate = x.UoMRate,
                Name = x.UoMName,
                Id = x.UoMId
            }
        }))];
     }

    public async Task<(IEnumerable<TransferOrderStatus> data, int count)> GetTransferOrderStatuses(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(TransferOrderStatus.Id)),
                ("name", nameof(TransferOrderStatus.Name))
            )
            .From("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<TransferOrderStatus>(netsuiteService);

        return (response.items, response.totalResults);
    }

    public async Task<bool> CreateStockTransferRequest(StockTransferRequestInfoDTO dto)
    {
        string payloadString = CreateSTRPayload(dto);

        var url = dto.TransferCategory.IsInterCompany ?
            $"{netsuiteService.GetRestAPIURI}/record/v1/interCompanyTransferOrder" :
            $"{netsuiteService.GetRestAPIURI}/record/v1/transferOrder";

        try
        {
            _ = await netsuiteService.MakeRequest<object>(url, payloadString, HttpMethod.Post);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }
        return true;
    }

    public async Task<bool> UpdateStockTransferRequest(StockTransferRequestInfoDTO dto)
    {
        string payloadString = CreateSTRPayload(dto);
        var url = dto.TransferCategory.IsInterCompany ?
            $"{netsuiteService.GetRestAPIURI}/record/v1/interCompanyTransferOrder/{dto.Id}" :
            $"{netsuiteService.GetRestAPIURI}/record/v1/transferOrder/{dto.Id}";

        try
        {
            _ = await netsuiteService.MakeRequest<object>(url, payloadString, HttpMethod.Patch);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }
        return true;
    }

    private StockTransferRequestDataGridDTO ConvertDataGridDTO(StockTransferRequestDataGridNSDTO nsdto)
    {

        var dto = nsdto.Adapt<StockTransferRequestDataGridDTO>();
        dto.Status = new TransferOrderStatus
        {
            Id = nsdto.StatusId,
            Name = nsdto.StatusName
        };
        return dto;
    }

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private string CreateSTRPayload(StockTransferRequestInfoDTO dto)
    {
        var anon = new
        {
            subsidiary = dto.Subsidiary != null ? new
            {
                id = dto.Subsidiary.Id.ToString()
            } : null,
            tosubsidiary = dto.ToSubsidiary != null && dto.TransferCategory.IsInterCompany ? new
            {
                id = dto.ToSubsidiary.Id.ToString()
            } : null,
            location = dto.SourceLocation != null ? new
            {
                id = dto.SourceLocation.Id.ToString()
            } : null,
            transferLocation = dto.DestinationLocation != null ? new
            {
                id = dto.DestinationLocation.Id.ToString()
            } : null,
            orderStatus = "A",
            custbody_dbti_transfer_category = new { id = dto.TransferCategory.Id },
            custbody_dbti_submitted_for_approval = true,
            custbody_dbti_prepared_by = dto.PreparedById,
            custbody_dbti_return_to_vendor = dto.TransferCategory.IsReturn && dto.Vendor != null ? new { id = dto.Vendor.Id.ToString() } : null,
            Department = new { id = "4" },
            Class = new { id = "1" },
            Memo = dto.Remarks,
            item = new
            {
                items = dto.Lines.Select(line =>
                {
                    return new
                    {
                        line = line.LineNumber,
                        item = new { id = line.ItemId },
                        quantity = line.QuantityAlloted,
                        department = new { id = "4" },
                        units = line.UoM?.Id.ToString() ?? null
                    };
                })
            }
        };

        return JsonSerializer.Serialize(anon, jsonSerializerOptions);
    }
}
