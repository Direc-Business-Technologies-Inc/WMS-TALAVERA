using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using Integration.NS.DataTransferObjects.InventoryAdjustment;
using Integration.NS.DataTransferObjects.InventoryTransferRequest;
using Integration.NS.Services;
using Mapster;
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
                ("BUILTIN.DF(t.custbody_dbti_itr_to_location)", nameof(InventoryTransferRequestNSDTO.DestinationLocationName)),
                ("t.custbody_dbti_itr_to_location", nameof(InventoryTransferRequestNSDTO.DestinationLocationId)),
                ("t.status", nameof(InventoryTransferRequestNSDTO.StatusId)),
                ("s.name", nameof(InventoryTransferRequestNSDTO.StatusName)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(InventoryTransferRequestNSDTO.Date))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline='T'")
            .LeftJoin("CustomTransactionType ct", "ct.scriptid = t.recordtype")
            .LeftJoin("transactionstatus s", "s.trantype = 'CuTrSale' AND t.status = s.id AND s.trancustomtype = ct.id")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "customsale_dbti_inv_transfer_req"),
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
            Status = new() { Id = result.StatusId, Name = result.StatusName }
        });
    }

    public async Task<IEnumerable<InventoryTransferRequestLineDTO>> GetInventoryTransferRequestLinesAsync(string Ref)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemId", nameof(InventoryTransferRequestLineNSDTO.ItemCode)),
                ("item.displayname", nameof(InventoryTransferRequestLineNSDTO.ItemDescription)),
                ("(tl.quantity / uom.conversionrate)", nameof(InventoryTransferRequestLineNSDTO.QuantityAlloted)),
                ("BUILTIN.DF(tl.units)", nameof(InventoryTransferRequestLineNSDTO.UoMName)),
                ("tl.units", nameof(InventoryTransferRequestLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(InventoryTransferRequestLineNSDTO.UoMRate)),
                ("iil.quantityonhand", nameof(InventoryTransferRequestLineNSDTO.QuantityOnHand))
            )
            .From("transactionline tl")
            .Join("item item", on: "item.id = tl.item")
            .Join("transaction t", on: "t.id = tl.transaction")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .LeftJoin("unitstypeuom uom", on: "tl.units = uom.internalid")
            .LeftJoin("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.tranid", Ref),
                DataGridFilterUtilities.Equal("t.recordtype", "customsale_dbti_inv_transfer_req"),
                DataGridFilterUtilities.Equal("tl.mainline", "F"),
                DataGridFilterUtilities.Equal("tl.accountinglinetype", "INCOME")
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryTransferRequestLineNSDTO>(query.Query);
        return response.items.Select(ConvertLine);
    }

    public async Task<(IEnumerable<InventoryTransferRequestDataGridDTO> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("t.id", nameof(InventoryTransferRequestDataGridDTO.Id)),
                ("t.memo", nameof(InventoryTransferRequestDataGridDTO.Memo)),
                ("t.tranid", nameof(InventoryTransferRequestDataGridDTO.ReferenceNumber)),
                ("BUILTIN.DF(tl.location)", nameof(InventoryTransferRequestDataGridDTO.SourceLocation)),
                ("BUILTIN.DF(t.subsidiary)", nameof(InventoryTransferRequestDataGridDTO.SubsidiaryName)),
                ("s.name", nameof(InventoryTransferRequestDataGridDTO.StatusName)),
                ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(InventoryTransferRequestDataGridDTO.PreparedBy)),
                ("BUILTIN.DF(t.custbody_dbti_itr_to_location)", nameof(InventoryTransferRequestDataGridDTO.DestinationLocation)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(InventoryTransferRequestDataGridDTO.Date))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline='T'")
            .LeftJoin("CustomTransactionType ct", "ct.scriptid = t.recordtype")
            .LeftJoin("transactionstatus s", "s.trantype = 'CuTrSale' AND t.status = s.id AND s.trancustomtype = ct.id")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "customsale_dbti_inv_transfer_req")
            )
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
            .From("CustomTransactionType ct")
            .Join("transactionstatus s", "s.trantype = 'CuTrSale' AND s.trancustomtype = ct.id")
            .WithDatagridIntent(intent)
            .WithFilter(
               DataGridFilterUtilities.Equal("ct.scriptid", "customsale_dbti_inv_transfer_req")
            )
            .Build();
        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryTransferRequestStatusDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<bool> CreateInventoryTransferRequest(InventoryTransferRequestDTO data)
    {
        var url = netsuiteService.GetRestletURI + "?script=1886&deploy=1";
        var payload = CreatePayload(data);

        try
        {
            //_ = await netsuiteService.MakeRequest<object>(url, payload, HttpMethod.Post);
            _ = await netsuiteService.MakeRequestOAuth1<object>(url, payload);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }

    public string CreatePayload(InventoryTransferRequestDTO data)
    {
        var anon = new
        {
            entity = data.Customer?.Id ?? null,
            subsidiary = data.Subsidiary?.Id ?? null,
            location = data.SourceLocation?.Id ?? null,
            custbody_dbti_itr_to_location = data.DestinationLocation?.Id ?? null,
            custbody_dbti_prepared_by = data.PreparedById,
            memo = data.Memo,
            trandate = data.Date,
            Class = 1,
            department = 4,
            lines = data.Lines.Where(x => x.QuantityAlloted > 0).Select(x => new
            {
                item = x.ItemID,
                quantity = x.QuantityAlloted,
                rate = x.Rate,
                units = x.UoM?.Id.ToString() ?? null,
                inventoryDetail = x.IsAllAssigned ? x.InventoryDetails.Select(y => new
                {
                    bin = y.Bin?.Id ?? null,
                    status = y.Status?.Id ?? null,
                    quantity = y.QuantityAlloted
                }): null
            })
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
            }
        });
    }
}
