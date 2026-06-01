using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Database.Libraries.Repositories;
using Integration.Sap.Repositories;

namespace Integration.SAP.Implementations.Others;

public class TransactionTypeIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions)
    : ITransactionTypeIntegration
{
    public async Task<IEnumerable<TransactionTypeSAPDTO>> GetTransactionTypesAsync()
    {
        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_Others_TransactionType", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for Transaction Types not found.");

        List<TransactionTypeSAPDTO> data = await SLActions.RawQueryAsync<TransactionTypeSAPDTO>(qry);

        return data;
    }
}
