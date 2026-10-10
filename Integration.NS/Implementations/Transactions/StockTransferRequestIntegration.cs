using Application.DataTransferObjects.Others;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.NS.Implementations.Transactions;

internal class StockTransferRequestIntegration(
    INetSuiteApiClientService netsuiteService,
    IHttpContextAccessor httpContextAccessor,
    SuiteQLQueryBuilderFactoryService builderFactory) : IStockTransferRequestIntegration
{
    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetIntercompanyTransferOrderList(DataGridIntent intent)
    {
        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)));

        // The legacy SuiteQL predicate (purchase_category != 3 OR
        // purchase_subcategory = 5) is intentionally NOT sent to NetSuite: it
        // resolves as a ~45s full scan (Q0=46s, Q5-without-it=1-2s,
        // Q6-date-bound=47-48s). The remaining base predicates are selective
        // (~1-2s), so we fetch the full base set (known-small: 25 rows in prod),
        // apply the 3VL-equivalent predicate in memory, then sort+page here.
        var baseRows = await FetchAllBaseRowsAsync(BuildIntercompanyBaseQuery(intent));

        var filtered = baseRows.Where(PassesPurchaseCategoryFilter).ToList();
        return SliceFilteredPage(filtered, intent);
    }

    private SuiteQLQueryBuilder BuildIntercompanyBaseQuery(DataGridIntent intent) =>
        builderFactory.Create()
                .Select(
                    ("t.id", nameof(StockTransferRequestDataGridNSDTO.Id)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.DateLastModified)),
                    ("t.tranid", nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_category)", nameof(StockTransferRequestDataGridNSDTO.PurchaseCategory)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_subcategory)", nameof(StockTransferRequestDataGridNSDTO.PurchaseSubcategory)),
                    ("t.custbody_dbti_purchase_category", nameof(StockTransferRequestDataGridNSDTO.PurchaseCategoryId)),
                    ("t.custbody_dbti_purchase_subcategory", nameof(StockTransferRequestDataGridNSDTO.PurchaseSubcategoryId)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridNSDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridNSDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridNSDTO.SourceLocation)),
                    ("s.name", nameof(StockTransferRequestDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestDataGridNSDTO.StatusId)),
                    ("CONCAT(re.firstname,CONCAT(' ',re.lastname))", nameof(StockTransferRequestDataGridNSDTO.RequestedBy)),
                    ("t.memo", nameof(StockTransferRequestDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .LeftJoin("employee re", on: "re.id = t.employee")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.Equal("t.recordtype", "intercompanytransferorder"),
                    DataGridFilterUtilities.Equal("t.custbody_dbti_transfer_category", 2)
                )
                .WithFromOrToSubsidiaries(httpContextAccessor, "t")
                .WithDatagridIntent(intent);

    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetReturnsList(DataGridIntent intent)
    {

        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)));
        var query = builderFactory.Create()
                .Select(
                    ("t.id", nameof(StockTransferRequestDataGridNSDTO.Id)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.DateLastModified)),
                    ("t.tranid", nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_category)", nameof(StockTransferRequestDataGridNSDTO.PurchaseCategory)),
                    ("BUILTIN.DF(t.custbody_dbti_transfer_category)", nameof(StockTransferRequestDataGridNSDTO.TransferCategory)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_subcategory)", nameof(StockTransferRequestDataGridNSDTO.PurchaseSubcategory)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.tosubsidiary)", nameof(StockTransferRequestDataGridNSDTO.ToSubsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridNSDTO.DestinationLocation)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridNSDTO.SourceLocation)),
                    ("s.name", nameof(StockTransferRequestDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestDataGridNSDTO.StatusId)),
                    ("CONCAT(re.firstname,CONCAT(' ',re.lastname))", nameof(StockTransferRequestDataGridNSDTO.RequestedBy)),
                    ("t.memo", nameof(StockTransferRequestDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .LeftJoin("employee re", on: "re.id = t.employee")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.In("t.recordtype", new string[] { "intercompanytransferorder", "transferorder" }),
                    DataGridFilterUtilities.In("t.custbody_dbti_transfer_category", new string[] { "3", "4" })
                )
                .WithFromOrToSubsidiaries(httpContextAccessor, "t")
                .WithDatagridIntent(intent)
                .Build();
        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridNSDTO>(query.Query, query.Limit, query.Offset);
        return (response.items.Select(ConvertDataGridDTO), response.totalResults);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetTransferOrderList(DataGridIntent intent)
    {

        if (intent.Sorts.Count == 0)
            intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)));

        // Same rationale as ICTO above: the purchase-category OR-group is a
        // ~45s full scan in NetSuite, the base predicates are ~1-2s.
        var baseRows = await FetchAllBaseRowsAsync(BuildTransferOrderBaseQuery(intent));

        var filtered = baseRows.Where(PassesPurchaseCategoryFilter).ToList();
        return SliceFilteredPage(filtered, intent);
    }

    private SuiteQLQueryBuilder BuildTransferOrderBaseQuery(DataGridIntent intent) =>
        builderFactory.Create()
                .Select(
                    ("t.id", nameof(StockTransferRequestDataGridNSDTO.Id)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.Date)),
                    ("TO_CHAR(t.lastmodifieddate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(StockTransferRequestDataGridNSDTO.DateLastModified)),
                    ("t.tranid", nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_category)", nameof(StockTransferRequestDataGridNSDTO.PurchaseCategory)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_subcategory)", nameof(StockTransferRequestDataGridNSDTO.PurchaseSubcategory)),
                    ("t.custbody_dbti_purchase_category", nameof(StockTransferRequestDataGridNSDTO.PurchaseCategoryId)),
                    ("t.custbody_dbti_purchase_subcategory", nameof(StockTransferRequestDataGridNSDTO.PurchaseSubcategoryId)),
                    ("CONCAT(e.firstname,CONCAT(' ',e.lastname))", nameof(StockTransferRequestDataGridNSDTO.PreparedBy)),
                    ("BUILTIN.DF(t.subsidiary)", nameof(StockTransferRequestDataGridNSDTO.Subsidiary)),
                    ("BUILTIN.DF(t.transferlocation)", nameof(StockTransferRequestDataGridNSDTO.DestinationLocation)),
                    ("s.name", nameof(StockTransferRequestDataGridNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestDataGridNSDTO.StatusId)),
                    ("BUILTIN.DF(tl.location)", nameof(StockTransferRequestDataGridNSDTO.SourceLocation)),
                    ("CONCAT(re.firstname,CONCAT(' ',re.lastname))", nameof(StockTransferRequestDataGridNSDTO.RequestedBy)),
                    ("t.memo", nameof(StockTransferRequestDataGridNSDTO.Remarks))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .LeftJoin("employee re", on: "re.id = t.employee")
                .WithFilters(
                    DataGridFilterUtilities.Equal("tl.mainline", "T"),
                    DataGridFilterUtilities.Equal("t.recordtype", "transferorder"),
                    DataGridFilterUtilities.Equal("t.custbody_dbti_transfer_category", 1)
                )
                .WithFromOrToSubsidiaries(httpContextAccessor, "t")
                .WithDatagridIntent(intent);

    /// <summary>
    /// Fetches every row of the selective base set, one 1000-row page per
    /// NetSuite call, capped at 5,000 rows. The base set is known-small in prod
    /// (25 rows), so this is normally ONE ~1-2s call. Server-side paging is
    /// bypassed on purpose: NetSuite would page the UNFILTERED base set, but
    /// the grid must page the FILTERED set (see SliceFilteredPage).
    /// </summary>
    private async Task<List<StockTransferRequestDataGridNSDTO>> FetchAllBaseRowsAsync(SuiteQLQueryBuilder baseBuilder)
    {
        const int PageSize = 1000;
        const int MaxRows = 5000;

        var all = new List<StockTransferRequestDataGridNSDTO>();
        var query = baseBuilder.Build();

        for (int offset = 0; offset < MaxRows; offset += PageSize)
        {
            var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridNSDTO>(
                query.Query, PageSize, offset);
            all.AddRange(response.items);
            if (response.items.Count < PageSize) break;
        }

        return all;
    }

    /// <summary>
    /// Orders the filtered set per the grid intent and slices the requested
    /// page. Sort keys mirror the server-side mapping (ReferenceNumber default;
    /// Date + ReferenceNumber from STRDataGrid), so page contents are stable.
    /// Beyond the fetch cap this degrades gracefully: pages are sliced from
    /// what was fetched rather than failing the grid.
    /// </summary>
    private static (IEnumerable<StockTransferRequestDataGridDTO> Data, int Count) SliceFilteredPage(
        List<StockTransferRequestDataGridNSDTO> filtered, DataGridIntent intent)
    {
        IEnumerable<StockTransferRequestDataGridNSDTO> ordered = filtered;
        var sorts = intent.Sorts.Count > 0
            ? intent.Sorts
            : [DataGridSortUtilities.Descending(nameof(StockTransferRequestDataGridNSDTO.ReferenceNumber))];

        foreach (var sort in sorts.AsEnumerable().Reverse())
        {
            Func<StockTransferRequestDataGridNSDTO, IComparable> key = sort.Property switch
            {
                nameof(StockTransferRequestDataGridNSDTO.Date) => r => r.Date,
                nameof(StockTransferRequestDataGridNSDTO.DateLastModified) => r => r.DateLastModified,
                nameof(StockTransferRequestDataGridNSDTO.StatusName) => r => r.StatusName,
                nameof(StockTransferRequestDataGridNSDTO.StatusId) => r => r.StatusId,
                _ => r => r.ReferenceNumber,
            };
            ordered = sort.Direction == SortDirectionEnum.Descending
                ? ordered.OrderByDescending(key)
                : ordered.OrderBy(key);
        }

        var orderedList = ordered.ToList();
        int take = intent.Take <= 0 ? 10 : intent.Take;
        var page = orderedList.Skip(intent.Skip).Take(take).ToList();
        return (page.Select(row => ConvertDataGridDTO(row)), orderedList.Count);
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
                    ("BUILTIN.DF(t.custbody_dbti_purchase_category)", nameof(StockTransferRequestHeaderNSDTO.PurchaseCategoryName)),
                    ("BUILTIN.DF(t.custbody_dbti_purchase_subcategory)", nameof(StockTransferRequestHeaderNSDTO.PurchaseSubcategoryName)),
                    ("t.custbody_dbti_return_to_vendor", nameof(StockTransferRequestHeaderNSDTO.VendorId)),
                    ("t.subsidiary", nameof(StockTransferRequestHeaderNSDTO.SubsidiaryId)),
                    ("t.tosubsidiary", nameof(StockTransferRequestHeaderNSDTO.ToSubsidiaryId)),
                    ("t.transferlocation", nameof(StockTransferRequestHeaderNSDTO.DestinationLocationId)),
                    ("tl.location", nameof(StockTransferRequestHeaderNSDTO.SourceLocationId)),
                    ("t.custbody_dbti_purchase_category", nameof(StockTransferRequestHeaderNSDTO.PurchaseCategoryId)),
                    ("t.custbody_dbti_purchase_subcategory", nameof(StockTransferRequestHeaderNSDTO.PurchaseSubcategoryId)),
                    ("t.memo", nameof(StockTransferRequestHeaderNSDTO.Remarks)),
                    ("t.custbody_dbti_transfer_category", nameof(StockTransferRequestHeaderNSDTO.TransferCategoryId)),
                    ("s.name", nameof(StockTransferRequestHeaderNSDTO.StatusName)),
                    ("s.id", nameof(StockTransferRequestHeaderNSDTO.StatusId)),
                    ("t.id", nameof(StockTransferRequestHeaderNSDTO.Id)),
                    ("t.custbody_dbti_submitted_for_approval", nameof(StockTransferRequestHeaderNSDTO.SubmittedForApprovals)),
                    ("BUILTIN.DF(t.custbody_dbti_transfer_category)", nameof(StockTransferRequestHeaderNSDTO.TransferCategoryName)),
                    ("re.firstname", nameof(StockTransferRequestHeaderNSDTO.RequestedByFirstName)),
                    ("re.lastname", nameof(StockTransferRequestHeaderNSDTO.RequestedByLastName)),
                    ("t.employee", nameof(StockTransferRequestHeaderNSDTO.RequestedById))
                )
                .From("transaction t")
                .Join("transactionline tl", on: "tl.transaction = t.id")
                .LeftJoin("CUSTOMLIST_DBTI_CR_APPROVAL_STATUSES s", on: "s.id = t.custbody_dbti_custom_approval_status")
                .LeftJoin("employee e", on: "e.id = t.custbody_dbti_prepared_by")
                .LeftJoin("employee re", on: "re.id = t.employee")
                //.WithSubsidiaries(httpContextAccessor, "t")
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

        dto.Vendor = nsdto.VendorId.HasValue && nsdto.VendorId.Value > 0 ? new() { Name = nsdto.VendorName ?? string.Empty, Id = nsdto.VendorId.Value } : null;
        dto.SourceLocation = new() { Name = nsdto.SourceLocationName, Id = nsdto.SourceLocationId };
        dto.SourceLocation = new() { Name = nsdto.SourceLocationName, Id = nsdto.SourceLocationId };
        dto.DestinationLocation = new() { Name = nsdto.DestinationLocationName, Id = nsdto.DestinationLocationId };
        dto.Subsidiary = new() { Name = nsdto.SubsidiaryName, Id = nsdto.SubsidiaryId };
        dto.ToSubsidiary = new() { Name = nsdto.ToSubsidiaryName, Id = nsdto.ToSubsidiaryId };
        dto.PurchaseCategory = new() { Name = nsdto.PurchaseCategoryName, Id = nsdto.PurchaseCategoryId };
        dto.PurchaseSubcategory = new() { Name = nsdto.PurchaseSubcategoryName, Id = nsdto.PurchaseSubcategoryId };
        dto.Status = new() { Name = nsdto.StatusName, Id = nsdto.StatusId };
        dto.RequestedBy = nsdto.RequestedById > 0
            ? new EmployeeNsDTO
            {
                NsId = nsdto.RequestedById,
                FirstName = nsdto.RequestedByFirstName,
                LastName = nsdto.RequestedByLastName
            }
            : null;
        dto.IsEditable = !nsdto.IsSubmittedForApprovals && (nsdto.StatusId == 3 || nsdto.StatusId == 2);
        dto.TransferCategory = nsdto.TransferCategoryId switch
        {
            1 => TransferCategory.Transfer,
            2 => TransferCategory.IntercompanyTransfer,
            3 => TransferCategory.ReturnsGood,
            4 => TransferCategory.ReturnsBad,
            _ => TransferCategory.Create(
                nsdto.TransferCategoryId, 
                nsdto.TransferCategoryName)
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
                ("pb.binnumber", nameof(StockTransferRequestLineNSDTO.PreferredBin)),
                ("item.displayname", nameof(StockTransferRequestLineNSDTO.ItemDescription)),
                ("tl.linesequencenumber", nameof(StockTransferRequestLineNSDTO.LineNumber)),
                ("(iil.quantityavailable / uom.conversionrate)", nameof(StockTransferRequestLineNSDTO.QuantityOnHand)),
                ("(-tl.quantity / uom.conversionrate)", nameof(StockTransferRequestLineNSDTO.QuantityAlloted)), // idk why this is negative
                ("BUILTIN.DF(tl.custcol_dbti_vendor)", nameof(StockTransferRequestLineNSDTO.VendorName)),
                ("tl.custcol_dbti_vendor", nameof(StockTransferRequestLineNSDTO.VendorId))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .Join("unitsTypeUom uom", on: "tl.units = uom.internalid")
            .Join("transactionline ml", on: "ml.transaction = t.id AND ml.mainline = 'T'")
            .Join("inventoryitemlocations iil", on: "tl.item = iil.item AND ml.location = iil.location")
            .LeftJoin($"{SuiteQLFragments.PreferredBin()} pb", on: "pb.item = item.id AND pb.location = ml.location")
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
            },
            Vendor = x.VendorId.HasValue ? new Application.DataTransferObjects.Others.VendorDTO { Id = x.VendorId.Value, Name = x.VendorName ?? string.Empty } : null
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
            $"{netsuiteService.GetRestAPIURI}/record/v1/interCompanyTransferOrder/{dto.Id}?replace=item" :
            $"{netsuiteService.GetRestAPIURI}/record/v1/transferOrder/{dto.Id}?replace=item";

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

    public async Task<bool> SubmitStockTransferRequestForApproval(StockTransferRequestInfoDTO dto)
    {
        string payloadString = "{\"custbody_dbti_submitted_for_approval\":true}";
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

    /// <summary>
    /// In-memory equivalent of the legacy SuiteQL predicate
    /// <c>(purchase_category != 3 OR purchase_subcategory = 5)</c>, which was
    /// removed from the TO/ICTO grid queries because NetSuite resolves that
    /// OR-group as a ~45s full scan (Q0=46s, Q5-without-it=1-2s, Q6-date-bound=47-48s).
    /// NULL semantics mirror SQL three-valued logic exactly: <c>NULL != 3</c>
    /// is UNKNOWN (not TRUE — C#'s lifted <c>!=</c> would wrongly return true
    /// for null, so the HasValue guard is load-bearing), and <c>NULL = 5</c> is
    /// UNKNOWN (C# <c>==</c> on null already yields false, matching SQL here).
    /// </summary>
    private static bool PassesPurchaseCategoryFilter(StockTransferRequestDataGridNSDTO row) =>
        (row.PurchaseCategoryId.HasValue && row.PurchaseCategoryId.Value != 3)
        || row.PurchaseSubcategoryId == 5;

    private static StockTransferRequestDataGridDTO ConvertDataGridDTO(StockTransferRequestDataGridNSDTO nsdto)
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
            custbody_dbti_prepared_by = dto.PreparedById,
            employee = dto.RequestedBy is not null ? new { id = dto.RequestedBy.NsId.ToString() } : null,
            custbody_dbti_return_to_vendor = dto.TransferCategory.IsReturn && dto.Vendor is not null && dto.Vendor.Id > 0 ? new { id = dto.Vendor.Id.ToString() } : null,
            custbody_dbti_purchase_category = dto.PurchaseCategory?.Id ?? null,
            custbody_dbti_purchase_subcategory = dto.PurchaseSubcategory?.Id ?? null,
            Department = new { id = "4" },
            Class = new { id = "1" },
            Memo = dto.Remarks,
            custbody_dbti_created_in_wms = true,
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
                         units = line.UoM?.Id.ToString() ?? null,
                         custcol_dbti_vendor = line.Vendor is not null ? new { id = line.Vendor.Id.ToString() } : null
                     };
                 })
             }
         };

         return JsonSerializer.Serialize(anon, jsonSerializerOptions);
     }
}
