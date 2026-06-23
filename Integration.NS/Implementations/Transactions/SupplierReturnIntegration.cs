using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Integration.NS.Helpers;
using Integration.NS.Services;
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
    public Task<SupplierReturnDTO?> GetReturnAsync()
    {
        throw new NotImplementedException();
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

    public Task<IEnumerable<SupplierReturnLineDTO>> GetReturnLinesAsync()
    {
        throw new NotImplementedException();
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
}
