using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Integration.NS.DataTransferObjects.StockTransferRequest;
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
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class SupplierReturnIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory
    ) : ISupplierReturnIntegration
{
    public async Task<SupplierReturnDTO?> GetReturnAsync(string referenceNumber)
    {
        var query = builderFactory.Create()
                .Select(
                    ("BUILTIN.DF(t.entity)", nameof(SupplierReturnNSDTO.VendorName)),
                    ("t.id", nameof(SupplierReturnNSDTO.Id)),
                    ("t.entity", nameof(SupplierReturnNSDTO.VendorId)),
                    ("BUILTIN.DF(t.custbody_dbti_return_category)", nameof(SupplierReturnNSDTO.CategoryName)),
                    ("t.custbody_dbti_return_category", nameof(SupplierReturnNSDTO.CategoryId)),
                    ("t.tranid", nameof(SupplierReturnNSDTO.ReferenceNumber)),
                    ("ml.location", nameof(SupplierReturnNSDTO.LocationId)),
                    ("t.subsidiary", nameof(SupplierReturnNSDTO.SubsidiaryId)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(SupplierReturnNSDTO.SubsidiaryName)),
                    ("BUILTIN.DF(ml.location)", nameof(SupplierReturnNSDTO.LocationName)),
                    ("t.memo", nameof(SupplierReturnNSDTO.Memo)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnNSDTO.Date)),
                    ("s.name", nameof(SupplierReturnNSDTO.StatusName)),
                    ("s.id", nameof(SupplierReturnNSDTO.StatusId)),
                    ("t.custbody_dbti_purchase_category", nameof(SupplierReturnNSDTO.PurchaseCategoryId)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_category)", nameof(SupplierReturnNSDTO.PurchaseCategoryName)),
                    ("t.custbody_dbti_purchase_subcategory", nameof(SupplierReturnNSDTO.PurchaseSubCategoryId)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_subcategory)", nameof(SupplierReturnNSDTO.PurchaseSubCategoryName)),
                    ("tf.tranid", nameof(SupplierReturnNSDTO.CreatedFrom)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(SupplierReturnNSDTO.PreparedBy))
                )
                .From("transaction t")
                .Join("VendorReturnAuthorizationStatus s", on: "t.status = s.id")
                .LeftJoin("transactionline ml", "ml.mainline = 'T' and ml.transaction = t.id")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .LeftJoin("transaction tf", on: "tf.id = ml.createdfrom")
                .WithSubsidiaries(httpContextAccessor, "t")
                .WithFilters(
                    DataGridFilterUtilities.Equal("t.recordtype", "vendorreturnauthorization"),
                    DataGridFilterUtilities.Equal("t.tranid", referenceNumber)
                )
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnNSDTO>(query.Query);
        var nsdto = response.items.FirstOrDefault();
        if (nsdto is null) return null;

        return nsdto.Adapt(new SupplierReturnDTO
        {
            Location = new() { Id = nsdto.LocationId, Name = nsdto.LocationName },
            ReturnCategory = new() { Id = nsdto.CategoryId, Name = nsdto.CategoryName },
            Status = new() { Id = nsdto.StatusId, Name = nsdto.StatusName },
            Vendor = new() { Id = nsdto.VendorId, Name = nsdto.VendorName },
            Subsidiary = new() { Id= nsdto.SubsidiaryId, Name = nsdto.SubsidiaryName },
            PurchaseCategory = new() { Id = nsdto.PurchaseCategoryId, Name = nsdto.PurchaseCategoryName},
            PurchaseSubcategory = new() { Id = nsdto.PurchaseSubCategoryId, PurchaseCategoryId = nsdto.PurchaseCategoryId, Name = nsdto.PurchaseSubCategoryName},
        });
    }

    public async Task<(IEnumerable<ReturnCategoryDTO> Data, int Count)> GetReturnCategories(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(ReturnCategoryDTO.Id)),
                ("name", nameof(ReturnCategoryDTO.Name))
            )
            .From("CUSTOMLIST_DBTI_RETURN_CATEGORY_LIST")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ReturnCategoryDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    public async Task<IEnumerable<SupplierReturnLineDTO>> GetReturnLinesAsync(string referenceNumber)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemid", nameof(SupplierReturnLineNSDTO.ItemCode)),
                ("uom.unitName", nameof(SupplierReturnLineNSDTO.UoMName)),
                ("uom.internalid", nameof(SupplierReturnLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(SupplierReturnLineNSDTO.UoMRate)),
                ("BUILTIN.DF(tl.location)", nameof(SupplierReturnLineNSDTO.LocationName)),
                ("tl.location", nameof(SupplierReturnLineNSDTO.LocationId)),
                ("item.displayname", nameof(SupplierReturnLineNSDTO.ItemDescription)),
                ("-(tl.quantity / uom.conversionrate)", nameof(SupplierReturnLineNSDTO.QuantityAlloted)),
                ("(iil.quantityavailable / uom.conversionrate)", nameof(SupplierReturnLineNSDTO.QuantityAvailable))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .LeftJoin("unitsTypeUom uom", on: "tl.units = uom.internalid")
            .LeftJoin("transactionline ml", on: "ml.mainline = 'T' AND ml.transaction = tl.transaction")
            .LeftJoin("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "vendorreturnauthorization"),
                DataGridFilterUtilities.Equal("t.tranid", referenceNumber)
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnLineNSDTO>(query.Query);
        return response.items.Select(ConvertLineDTO);
    }

    public async Task<(IEnumerable<SupplierReturnDataGridDTO> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(SupplierReturnDataGridDTO.ReferenceNumber)));
        var query = builderFactory.Create()
                .Select(
                    ("BUILTIN.DF(t.entity)", nameof(SupplierReturnDataGridDTO.VendorName)),
                    ("BUILTIN.DF(t.custbody_dbti_return_category)", nameof(SupplierReturnDataGridDTO.CategoryName)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(SupplierReturnDataGridDTO.PreparedBy)),
                    ("t.tranid", nameof(SupplierReturnDataGridDTO.ReferenceNumber)),
                    ("tfrom.tranid", nameof(SupplierReturnDataGridDTO.CreatedFrom)),
                    ("t.memo", nameof(SupplierReturnDataGridDTO.Memo)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnDataGridDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnDataGridDTO.DateLastModified)),
                    ("s.name", nameof(SupplierReturnDataGridDTO.StatusName))
                )
                .From("transaction t")
                .Join("VendorReturnAuthorizationStatus s", on: "t.status = s.id")
                .LeftJoin("transactionline ml", on: "ml.mainline = 'T' and t.id = ml.transaction")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .LeftJoin("transaction tfrom", on: "tfrom.id = ml.createdfrom")
                .WithSubsidiaries(httpContextAccessor, "t")
                .WithFilter(DataGridFilterUtilities.Equal("t.recordtype", "vendorreturnauthorization"))
                .WithDatagridIntent(intent)
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnDataGridDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<ReturnStatusDTO> Data, int Count)> GetReturnStatuses(DataGridIntent intent)
    {
        var query = builderFactory.Create()
            .Select(
                ("id", nameof(ReturnStatusDTO.Id)),
                ("name", nameof(ReturnStatusDTO.Name))
            )
            .From("VendorReturnAuthorizationStatus")
            .WithDatagridIntent(intent)
            .Build();

        var response = await query.ExecuteWithPaging<ReturnStatusDTO>(netsuiteService);

        return (response.items, response.totalResults);
    }

    public async Task<bool> CreateSupplierReturn(SupplierReturnDTO data)
    {
        if (data.SourcePO is null) return await CreateNewSupplierReturn(data);
        return await CreateSupplierReturnFromPurchaseOrder(data);
    }

    public async Task<bool> CreateNewSupplierReturn(SupplierReturnDTO data)
    {
        string payload = CreatePayload(data);
        var uri = netsuiteService.GetRestAPIURI + "/record/v1/vendorReturnAuthorization";

        try
        {
            _ = await netsuiteService.MakeRequest<object>(uri, payload, HttpMethod.Post);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }
    public async Task<bool> CreateSupplierReturnFromPurchaseOrder(SupplierReturnDTO data)
    {
        if (data.SourcePO is null) throw new InvalidOperationException("INTERNAL ERROR: No purchase order reference given");
        var payload = new
        {
            custbody_dbti_return_category = data.ReturnCategory?.Id ?? null,
            memo = data.Memo,
        };
        var payloadString = JsonSerializer.Serialize(payload, jsonOpts);
        var uri = netsuiteService.GetRestAPIURI + $"/record/v1/purchaseOrder/{data.SourcePO}/!transform/vendorReturnAuthorization";

        try
        {
            _ = await netsuiteService.MakeRequest<object>(uri, payloadString, HttpMethod.Post);
        }
        catch (Exception ex) when (ex.Message.Equals("Empty response from NetSuite API", StringComparison.OrdinalIgnoreCase))
        {
            // Empty response is but http response is a success status code
        }

        return true;
    }


    public async Task<SupplierReturnDTO?> GetReturnFromPurchaseOrderAsync(string purchaseOrderId)
    {
        var query = builderFactory.Create()
                .Select(
                    ("t.id", nameof(SupplierReturnNSDTO.SourcePO)),
                    ("BUILTIN.DF(t.entity)", nameof(SupplierReturnNSDTO.VendorName)),
                    ("t.entity", nameof(SupplierReturnNSDTO.VendorId)),
                    ("t.tranid", nameof(SupplierReturnNSDTO.ReferenceNumber)),
                    ("ml.location", nameof(SupplierReturnNSDTO.LocationId)),
                    ("t.subsidiary", nameof(SupplierReturnNSDTO.SubsidiaryId)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(SupplierReturnNSDTO.SubsidiaryName)),
                    ("BUILTIN.DF(ml.location)", nameof(SupplierReturnNSDTO.LocationName)),
                    ("categ.id", nameof(SupplierReturnNSDTO.PurchaseCategoryId)),
                    ("categ.name", nameof(SupplierReturnNSDTO.PurchaseCategoryName)),
                    ("subcat.id", nameof(SupplierReturnNSDTO.PurchaseSubCategoryId)),
                    ("subcat.name", nameof(SupplierReturnNSDTO.PurchaseSubCategoryName)),
                    ("t.memo", nameof(SupplierReturnNSDTO.Memo)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnNSDTO.Date))
                )
                .From("transaction t")
                .Join("VendorReturnAuthorizationStatus s", "t.status = s.id")
                .LeftJoin("transactionline ml", "ml.mainline = 'T' and ml.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_PURCHASE_CATEGORY_LIST categ", "t.custbody_dbti_purchase_category = categ.id")
                .LeftJoin("CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES subcat", "t.custbody_dbti_purchase_subcategory = subcat.id")
                .WithSubsidiaries(httpContextAccessor, "t")
                .WithFilters(
                    DataGridFilterUtilities.Equal("t.recordtype", "purchaseorder"),
                    DataGridFilterUtilities.Equal("t.status", "F"),
                    DataGridFilterUtilities.Equal("t.tranid", purchaseOrderId)
                )
                .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnNSDTO>(query.Query);
        var nsdto = response.items.FirstOrDefault();
        if (nsdto is null) return null;

        return nsdto.Adapt(new SupplierReturnDTO
        {
            Location = new() { Id = nsdto.LocationId, Name = nsdto.LocationName },
            Vendor = new() { Id = nsdto.VendorId, Name = nsdto.VendorName },
            Subsidiary = new() { Id = nsdto.SubsidiaryId, Name = nsdto.SubsidiaryName },
            PurchaseCategory = new() { Id = nsdto.PurchaseCategoryId, Name = nsdto.PurchaseCategoryName },
            PurchaseSubcategory = new() { Id = nsdto.PurchaseSubCategoryId, Name = nsdto.PurchaseSubCategoryName, PurchaseCategoryId = nsdto.PurchaseCategoryId },
        });
    }

    public async Task<IEnumerable<SupplierReturnLineDTO>> GetReturnFromPurchaseOrderLinesAsync(string purchaseOrderId)
    {
        var query = builderFactory.Create()
            .Select(
                ("item.itemid", nameof(SupplierReturnLineNSDTO.ItemCode)),
                ("item.id", nameof(SupplierReturnLineNSDTO.ItemId)),
                ("tl.id", nameof(SupplierReturnLineNSDTO.LineNumber)),
                ("uom.unitName", nameof(SupplierReturnLineNSDTO.UoMName)),
                ("uom.internalid", nameof(SupplierReturnLineNSDTO.UoMId)),
                ("uom.conversionrate", nameof(SupplierReturnLineNSDTO.UoMRate)),
                ("BUILTIN.DF(tl.location)", nameof(SupplierReturnLineNSDTO.LocationName)),
                ("tl.location", nameof(SupplierReturnLineNSDTO.LocationId)),
                ("item.displayname", nameof(SupplierReturnLineNSDTO.ItemDescription)),
                ("(iil.quantityavailable / uom.conversionrate)", nameof(SupplierReturnLineNSDTO.QuantityAvailable)),
                ("(tl.quantity / uom.conversionrate)", nameof(SupplierReturnLineNSDTO.QuantityAlloted))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .LeftJoin("inventoryitemlocations iil", on: "tl.item = iil.item AND tl.location = iil.location")
            .LeftJoin("unitsTypeUom uom", on: "tl.units = uom.internalid")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "purchaseorder"),
                DataGridFilterUtilities.Equal("t.tranid", purchaseOrderId)
            )
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<SupplierReturnLineNSDTO>(query.Query);
        return response.items.Select(ConvertLineDTO);
    }

    public async Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()
            .Select(
                ("t.id", nameof(PurchaseOrderDataGridDTO.Id)),
                ("t.tranid", nameof(PurchaseOrderDataGridDTO.ReferenceNumber)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(PurchaseOrderDataGridDTO.Date)),
                ("TO_CHAR(t.custbody_dbti_order_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(PurchaseOrderDataGridDTO.DeliveryDate)),
                ("BUILTIN.DF(tl.entity)", nameof(PurchaseOrderDataGridDTO.VendorName)),
                ("t.memo", nameof(PurchaseOrderDataGridDTO.Memo)),
                ("s.name", nameof(PurchaseOrderDataGridDTO.Status)),
                ("s.id", nameof(PurchaseOrderDataGridDTO.StatusId))
            )
            .From("transaction t")
            .WithSubsidiaries(httpContextAccessor, "t")
            .LeftJoin("transactionline tl", on: "t.id = tl.transaction AND tl.mainline = 'T'")
            .LeftJoin("purchaseorderstatus s", on: "t.status = s.id")
            .WithDatagridIntent(intent)
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "purchaseorder"),
                DataGridFilterUtilities.In("t.status", new string[] {"F", "G"})
            );

        SuiteQLQuery query = builder.Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<PurchaseOrderDataGridDTO>(query.Query, limit: query.Limit, offset: query.Offset);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<PurchaseCategoryDTO>, int count)> GetPurchaseCategoriesAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()
            .Select(
                ("id", nameof(PurchaseCategoryDTO.Id)),
                ("name", nameof(PurchaseCategoryDTO.Name))
            )
            .From("CUSTOMLIST_DBTI_PURCHASE_CATEGORY_LIST")
            .WithDatagridIntent(intent);

        var response = await builder.Build().ExecuteWithPaging<PurchaseCategoryDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    public async Task<(IEnumerable<PurchaseSubCategoryDTO>, int count)> GetPurchaseSubcategoriesAsync(DataGridIntent intent)
    {
        var builder = builderFactory.Create()   
            .Select(
                ("id", nameof(PurchaseSubCategoryDTO.Id)),
                ("name", nameof(PurchaseSubCategoryDTO.Name)),
                ("custrecord_dbti_psc_purchase_category", nameof(PurchaseSubCategoryDTO.PurchaseCategoryId))
            )
            .From("CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES")
            .WithDatagridIntent(intent);

        var response = await builder.Build().ExecuteWithPaging<PurchaseSubCategoryDTO>(netsuiteService);
        return (response.items, response.totalResults);
    }

    private string CreatePayload(SupplierReturnDTO data)
    {
        var anon = new
        {
            entity = data.Vendor?.Id ?? null,
            location = data.Location?.Id ?? null,
            department = 15, //operations
            Class = 1, //external
            subsidiary = data.Subsidiary?.Id ?? null,
            custbody_dbti_prepared_by = data.PreparedById,
            custbody_dbti_return_category = data.ReturnCategory?.Id ?? null,
            custbody_dbti_purchase_category = data.PurchaseSubcategory != null ? data.PurchaseSubcategory.PurchaseCategoryId : data.PurchaseCategory?.Id ?? null,
            custbody_dbti_purchase_subcategory = data.PurchaseSubcategory?.Id ?? null,
            memo = data.Memo,       
            orderStatus = "A",
            item = new
            {
                items = data.Lines.Select(x => new
                {
                    item = x.ItemId,
                    quantity = x.QuantityAlloted,
                    department = 15, //operations
                    units = x.UoM?.Id.ToString() ?? null
                })
            }
        };

        return JsonSerializer.Serialize(anon, jsonOpts);
    }

    private readonly JsonSerializerOptions jsonOpts = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private SupplierReturnLineDTO ConvertLineDTO (SupplierReturnLineNSDTO nsdto)
    {
        return nsdto.Adapt(new SupplierReturnLineDTO() 
        { 
            UoM = new() { Id = nsdto.UoMId, Name = nsdto.UoMName, ConversionRate = nsdto.UoMRate },
            Location = new () { Id = nsdto.LocationId, Name = nsdto.LocationName }
        });
    }
}
