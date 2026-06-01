using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ITransactionTypeHandler
{
    Task<IEnumerable<TransactionTypeVM>> GetTransactionTypesAsync();
}
