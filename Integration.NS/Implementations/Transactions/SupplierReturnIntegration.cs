using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Integration.NS.DataTransferObjects.StockTransferRequest;
using Integration.NS.DataTransferObjects.SupplierReturn;
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

public class SupplierReturnIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory
    ) : ISupplierReturnIntegration
{
    public async Task<SupplierReturnDTO?> GetReturnAsync(string referenceNumber)
    {
        var query = builderFactory.Create()
                .Select(
                    ("BUILTIN.DF(t.entity)", nameof(SupplierReturnNSDTO.VendorName)),
                    ("t.entity", nameof(SupplierReturnNSDTO.VendorId)),
                    ("BUILTIN.DF(t.custbody_dbti_return_category)", nameof(SupplierReturnNSDTO.CategoryName)),
                    ("t.custbody_dbti_return_category", nameof(SupplierReturnNSDTO.CategoryId)),
                    ("t.tranid", nameof(SupplierReturnNSDTO.ReferenceNumber)),
                    ("t.location", nameof(SupplierReturnNSDTO.LocationId)),
                    ("BUILTIN.DF(t.location)", nameof(SupplierReturnNSDTO.LocationName)),
                    ("t.memo", nameof(SupplierReturnNSDTO.Memo)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnNSDTO.Date)),
                    ("s.name", nameof(SupplierReturnNSDTO.StatusName)),
                    ("s.id", nameof(SupplierReturnNSDTO.StatusId))
                )
                .From("transaction t")
                .Join("VendorReturnAuthorizationStatus s", on: "t.status = s.id")
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
                ("(tl.quantity / uom.conversionrate)", nameof(SupplierReturnLineNSDTO.QuantityAlloted))
            )
            .From("transactionline tl")
            .Join("transaction t", on: "tl.transaction = t.id")
            .Join("item", on: "tl.item = item.id")
            .Join("unitsTypeUom uom", on: "tl.units = uom.internalid")
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
        var query = builderFactory.Create()
                .Select(
                    ("BUILTIN.DF(t.entity)", nameof(SupplierReturnDataGridDTO.VendorName)),
                    ("BUILTIN.DF(t.custbody_dbti_return_category)", nameof(SupplierReturnDataGridDTO.CategoryName)),
                    ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(SupplierReturnDataGridDTO.PreparedBy)),
                    ("t.tranid", nameof(SupplierReturnDataGridDTO.ReferenceNumber)),
                    ("t.memo", nameof(SupplierReturnDataGridDTO.Memo)),
                    ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(SupplierReturnDataGridDTO.Date)),
                    ("s.name", nameof(SupplierReturnDataGridDTO.StatusName))
                )
                .From("transaction t")
                .Join("VendorReturnAuthorizationStatus s", on: "t.status = s.id")
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

    private SupplierReturnLineDTO ConvertLineDTO (SupplierReturnLineNSDTO nsdto)
    {
        return nsdto.Adapt(new SupplierReturnLineDTO() 
        { 
            UoM = new() { Id = nsdto.UoMId, Name = nsdto.UoMName, ConversionRate = nsdto.UoMRate },
            Location = new () { Id = nsdto.LocationId, Name = nsdto.LocationName }
        });
    }
}
