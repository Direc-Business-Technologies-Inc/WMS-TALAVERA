using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Integration.NS.Services;
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

        var response = await netsuiteService.ExecuteSuiteQLQuery<StockTransferRequestDataGridDTO>(query.Query, query.Limit, query.Offset);

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
}
