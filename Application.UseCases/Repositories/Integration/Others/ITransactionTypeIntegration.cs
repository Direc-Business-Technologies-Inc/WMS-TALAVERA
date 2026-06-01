using Application.DataTransferObjects.Others.SAP;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ITransactionTypeIntegration
{
    Task<IEnumerable<TransactionTypeSAPDTO>> GetTransactionTypesAsync();
}
