using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class InventoryTransferRequestIntegration(
    INetSuiteApiClientService netsuiteService,
    SuiteQLQueryBuilderFactoryService builderFactory)
    : IInventoryTransferRequestIntegration
{
    public Task<InventoryTransferRequestDTO?> GetInventoryTransferRequestAsync(string Ref)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<InventoryTransferRequestLineDTO>> GetInventoryTransferRequestLinesAsync(string Ref)
    {
        throw new NotImplementedException();
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
                ("BUILTIN.DF(t.custbody_dbti_prepared_by)", nameof(InventoryTransferRequestDataGridDTO.PreparedBy)),
                ("BUILTIN.DF(t.custbody_dbti_itr_to_location)", nameof(InventoryTransferRequestDataGridDTO.DestinationLocation)),
                ("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", nameof(InventoryTransferRequestDataGridDTO.Date))
            )
            .From("transaction t")
            .Join("transactionline tl", "tl.transaction = t.id AND tl.mainline='T'")
            .WithFilters(
                DataGridFilterUtilities.Equal("t.recordtype", "customsale_dbti_inv_transfer_req")
            )
            .WithDatagridIntent(intent)
            .Build();

        var response = await netsuiteService.ExecuteSuiteQLQuery<InventoryTransferRequestDataGridDTO>(query.Query, query.Limit, query.Offset);
        return (response.items, response.totalResults);
    }
}
