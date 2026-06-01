using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.UseCases.Repositories.Integration.Transaction.GoodsIssue;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class GoodsIssueIntegration : IGoodsIssueIntegration
{
    public Task<(IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count)> GetApprovedGoodsIssueDataGrid(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<GoodsIssueDataGridSAPDTO> Data, int Count)> GetGoodsIssueDraftDataGrid(DataGridIntent intent, string status)
    {
        throw new NotImplementedException();
    }

    public Task<GoodsIssueHeaderSAPDTO?> GetGoodsIssueDraftHeader(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<GoodsIssueLineSAPDTO>> GetGoodsIssueDraftLines(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<GoodsIssueHeaderSAPDTO?> GetGoodsIssueHeader(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<GoodsIssueLineSAPDTO>> GetGoodsIssueLines(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostGoodsIssue(GoodsIssueDTO data)
    {
        throw new NotImplementedException();
    }
}
