using Application.DataTransferObjects.Transactions.StockTransferRequest;

namespace Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;

public interface IAutoStrNetSuiteIntegration
{
    Task<IReadOnlyList<AutoStrOrderDTO>> DiscoverApprovedAsync(int approvalStatusId, string approvalTimeField,
        IReadOnlyList<int> subsidiaryIds, DateTime changedSinceUtc, CancellationToken ct);
    Task<AutoStrOrderDTO?> GetCurrentOrderAsync(int orderId, string approvalTimeField, CancellationToken ct);
    Task<(IReadOnlyList<AutoStrLineDTO> Lines, int OpenCount)> GetCurrentLinesAsync(
        AutoStrOrderDTO order, string? lineStatusField, bool categoryPolicyVerified, CancellationToken ct);
    Task<IReadOnlyList<int>> GetFulfillmentIdsAsync(int orderId, CancellationToken ct);
    Task PostShippedAsync(IReadOnlyList<AutoStrLineDTO> lines, CancellationToken ct);
}
