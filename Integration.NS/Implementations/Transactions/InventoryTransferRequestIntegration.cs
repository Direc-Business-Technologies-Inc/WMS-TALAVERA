using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using Integration.NS.DataTransferObjects.InventoryAdjustment;
using Integration.NS.DataTransferObjects.InventoryTransferRequest;
using Integration.NS.DataTransferObjects.SupplierReturn;
using Integration.NS.Helpers;
using Integration.NS.Services;
using Mapster;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class InventoryTransferRequestIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory)
    : IInventoryTransferRequestIntegration
{
    public async Task<InventoryTransferRequestDTO?> GetInventoryTransferRequestAsync(string Ref)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryTransferRequestNSDTO.Id)),
                ("t.memo", nameof(InventoryTransferRequestNSDTO.Memo)),
                ("t.tranid", nameof(InventoryTransferRequestNSDTO.ReferenceNumber)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryTransferRequestNSDTO.SourceLocationName)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryTransferRequestNSDTO.SubsidiaryName)),
                ("BUILTIN.DF(t.entity)", nameof(InventoryTransferRequestNSDTO.CustomerName)),
                ("t.subsidiary", nameof(InventoryTransferRequestNSDTO.SubsidiaryId)),
                ("tl.location", nameof(InventoryTransferRequestNSDTO.SourceLocationId)),
                ("tl.entity", nameof(InventoryTransferRequestNSDTO.CustomerId)),
                ("s.id", nameof(InventoryTransferRequestNSDTO.StatusId)),
                ("s.name", nameof(InventoryTransferRequestNSDTO.StatusName)),
                ("BUILTIN.DF(t.transferlocation)", nameof(InventoryTransferRequestNSDTO.DestinationLocationName)),
                ("t.transferlocation", nameof(InventoryTransferRequestNSDTO.DestinationLocationId)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(InventoryTransferRequestNSDTO.Date)),
                ("t.custbody_dbti_submitted_for_approval", nameof(InventoryTransferRequestNSDTO.SubmittedForApproval)),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(InventoryTransferRequestNSDTO.PreparedBy))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline='T'")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
            .WithSubsidiaries(httpContextAccessor, "t")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "inventorytransfer"),
                DataGridFilterUtilities.Equal("t.tranid", Ref)
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryTransferRequestNSDTO>(query.Query);
        var result = response.items.FirstOrDefault();
        if (result is null) return null;

        return result.Adapt(new InventoryTransferRequestDTO
        {
            SourceLocation = new() { Id = result.SourceLocationId, Name = result.SourceLocationName },
            DestinationLocation = new() { Id = result.DestinationLocationId, Name = result.DestinationLocationName },
            Subsidiary = new() { Id = result.SubsidiaryId, Name = result.SubsidiaryName },
            Status = new() { Id = result.StatusId, Name = result.StatusName },
            IsEditable = !result.IsSubmittedForApproval && (result.StatusId == 2 || result.StatusId == 3), // status == draft || status == rejected
        });
    }

    public async Task<IEnumerable<InventoryTransferRequestLineDTO>> GetInventoryTransferRequestLinesAsync(string Ref)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemId", nameof(InventoryTransferRequestLineNSDTO.ItemCode)),
                ("item.id", nameof(InventoryTransferRequestLineNSDTO.ItemID)),
                ("item.displayname", nameof(InventoryTransferRequestLineNSDTO.ItemDescription)),
                ("(tl.quantity / uom.conversionrate)", nameof(InventoryTransferRequestLineNSDTO.QuantityAlloted)),
                ("BUILTIN.DF(tl.units)", nameof(InventoryTransferRequestLineNSDTO.UoMName)),
                ("tl.units", nameof(InventoryTransferRequestLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(InventoryTransferRequestLineNSDTO.UoMRate)),
                ("iil.quantityonhand", nameof(InventoryTransferRequestLineNSDTO.QuantityOnHand)),
                ("tl.location", nameof(InventoryTransferRequestLineNSDTO.LocationId)),
                ("stl.id", nameof(InventoryTransferRequestLineNSDTO.SourceLine)),
                ("tl.linesequencenumber", nameof(InventoryTransferRequestLineNSDTO.LineNumber)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryTransferRequestLineNSDTO.LocationName))
            )
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .Join("transactionline stl", on: "stl.transaction = t.id AND stl.displayline = tl.id")
            .LeftJoin("unitstypeuom uom", on: "tl.units = uom.internalid")
            .LeftJoin("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.tranid", Ref),
                DataGridFilterUtilities.Equal("t.recordtype", "inventorytransfer"),
                DataGridFilterUtilities.Equal("tl.mainline", "F")
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryTransferRequestLineNSDTO>(query.Query);
        return response.items.Select(ConvertLine);
    }

    public async Task<(IEnumerable<InventoryTransferRequestDataGridDTO> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(InventoryTransferRequestDataGridDTO.ReferenceNumber)));

        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryTransferRequestDataGridDTO.Id)),
                ("t.memo", nameof(InventoryTransferRequestDataGridDTO.Memo)),
                ("t.tranid", nameof(InventoryTransferRequestDataGridDTO.ReferenceNumber)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryTransferRequestDataGridDTO.SourceLocation)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryTransferRequestDataGridDTO.SubsidiaryName)),
                ("BUILTIN.DF(t.custbody_dbti_custom_approval_status)", nameof(InventoryTransferRequestDataGridDTO.StatusName)),
                ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(InventoryTransferRequestDataGridDTO.PreparedBy)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(InventoryTransferRequestDataGridDTO.Date)),
                ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(InventoryTransferRequestDataGridDTO.DateLastModified)),
                ("(SELECT TOP 1 BUILTIN.DF(pien.location) FROM transactionline pien WHERE pien.transaction = t.id AND pien.mainline='F')", nameof(InventoryTransferRequestDataGridDTO.DestinationLocation))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline='T'")
            .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "inventorytransfer")
            )
            .WithSubsidiaries(httpContextAccessor, "t")
            .WithDatagridIntent(intent)
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryTransferRequestDataGridDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<InventoryTransferRequestStatusDTO>, int)> GetStatusTypesAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("s.id", nameof(InventoryTransferRequestStatusDTO.Id)),
                ("s.name", nameof(InventoryTransferRequestStatusDTO.Name))
            )
            .From("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<InventoryTransferRequestStatusDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    public async Task<bool> CreateInventoryTransferRequest(InventoryTransferRequestDTO data)
    {
        var url = netsuiteService.GetRestAPIURI + "/record/v1/inventoryTransfer";
        var payload = CreatePayload(data);

        try
        {
            _ = await netsuiteService.MakeRequest<object>(url, payload, HttpMethod.Post);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }

    public async Task<bool> SubmitInventoryTransferRequestForApproval(InventoryTransferRequestDTO data)
    {
        var url = netsuiteService.GetRestAPIURI + $"/record/v1/inventoryTransfer/{data.Id}";
        var anon = new
        {
            custbody_dbti_submitted_for_approval = "T",
        };
        var payload = JsonSerializer.Serialize(anon, jsonOpts);

        try
        {
            _ = await netsuiteService.MakeRequest<object>(url, payload, HttpMethod.Patch);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }

    public async Task<bool> UpdateInventoryTransferRequest(InventoryTransferRequestDTO data)
    {
        var url = netsuiteService.GetRestAPIURI + $"/record/v1/inventoryTransfer/{data.Id}?replace=inventory";
        var payload = CreatePayload(data, true);

        try
        {
            _ = await netsuiteService.MakeRequest<object>(url, payload, HttpMethod.Patch);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }

    public string CreatePayload(InventoryTransferRequestDTO data, bool setStatus = false)
    {
        var anon = new
        {
            subsidiary = data.Subsidiary?.Id ?? null,
            location = data.SourceLocation?.Id ?? null,
            transferlocation = data.DestinationLocation?.Id ?? null,
            custbody_dbti_prepared_by = data.PreparedById,
            memo = data.Memo,
            trandate = data.Date,
            Class = 1, // external
            department = 15, //operations
            custbody_dbti_created_in_wms = true,
            inventory = new
            {
                items = data.Lines.Where(x => x.QuantityAlloted > 0).Select(x => new
                {
                    line = x.LineNumber,
                    item = x.ItemID,
                    adjustQtyBy = x.QuantityAlloted,
                    fromBinNumbers =  x.InventoryDetails.Any() ?
                        string.Join(",", x.InventoryDetails.Where(y => y.Bin is not null).Select(y => y.Bin?.Id)) :
                        null,
                    units = x.UoM?.Id.ToString() ?? null,
                    inventoryDetail = x.InventoryDetails.Any() ? new
                    {
                        InventoryAssignment = new
                        {
                            items = x.IsAllAssigned ? x.InventoryDetails.Select(y => new
                            {
                                binNumber = y.Bin?.Id ?? null,
                                inventoryStatus = y.Status?.Id ?? null,
                                quantity = y.QuantityAlloted
                            }) : null,
                        }
                    } : null,
                })
            } 
        };

        return JsonSerializer.Serialize(anon, jsonOpts);
    }


    readonly JsonSerializerOptions jsonOpts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };


    public InventoryTransferRequestLineDTO ConvertLine(InventoryTransferRequestLineNSDTO line)
    {
        return line.Adapt(new InventoryTransferRequestLineDTO()
        {
            UoM = new()
            {
                ConversionRate = line.UoMRate,
                Id = line.UoMId,
                Name = line.UoMName
            },
            Location = new()
            {
                Id = line.LocationId,
                Name = line.LocationName,
            }
        });
    }
}
