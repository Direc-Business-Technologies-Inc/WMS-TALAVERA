using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Commands.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Domain.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;

namespace Api.CoreWebAPI.Services;

/// <summary>
/// Polling is off unless the account-specific approval, condition and print
/// contracts are explicitly verified. All instances share the SQL claim.
/// </summary>
public sealed class StrAutoFulfillmentWorker(
    IServiceScopeFactory scopes,
    StrAutoFulfillmentOptions options,
    ILogger<StrAutoFulfillmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Ready)
        {
            logger.LogInformation("STR auto-fulfillment worker disabled or awaiting verified NetSuite contract: {ReadinessIssues}",
                string.Join(" ", options.GetReadinessIssues()));
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "STR auto-fulfillment discovery failed; no unsafe fallback posting will occur");
            }
            await Task.Delay(TimeSpan.FromSeconds(options.PollSeconds), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var rules = scope.ServiceProvider.GetRequiredService<IStockTransferRequestIntegration>();
        var store = scope.ServiceProvider.GetRequiredService<IStrAutoFulfillmentStore>();
        var integration = scope.ServiceProvider.GetRequiredService<IAutoStrNetSuiteIntegration>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Route activation is recorded before discovery. An already-approved
        // STR cannot suddenly ship because a new Yes/Yes rule was entered.
        DateTime? oldestCutoff = null;
        var activeRules = await rules.GetActiveFulfillmentExemptionRules();
        foreach (var rule in activeRules)
        {
            if (rule.IsInactive || rule.AutomatedPackingValue != options.YesListValueId ||
                rule.AutomatedShippingValue != options.YesListValueId ||
                !int.TryParse(rule.TransferCategoryId, out var category) ||
                !int.TryParse(rule.FromLocationId, out var from) ||
                !int.TryParse(rule.ToLocationId, out var to) ||
                string.IsNullOrWhiteSpace(rule.LabelTemplateId)) continue;
            // Use the same evaluator as the posting handler: conflicting rules
            // must not activate a route that can never safely be fulfilled.
            var evaluation = FulfillmentExemptionRuleEvaluator.Evaluate(activeRules,
                rule.TransferCategoryId, rule.FromLocationId, rule.ToLocationId, options.YesListValueId);
            if (evaluation.Decision != FulfillmentExemptionDecision.Eligible) continue;
            var cutoff = await store.GetOrCreateRouteCutoffAsync($"{category}:{from}:{to}", ct);
            if (oldestCutoff is null || cutoff < oldestCutoff) oldestCutoff = cutoff;
        }
        if (oldestCutoff is null) return;

        // Re-scan from the earliest route cutoff rather than moving a volatile
        // in-memory watermark past approvals during outages or restarts.
        IReadOnlyList<AutoStrOrderDTO> candidates = await integration.DiscoverApprovedAsync(
            options.ApprovedStatusId, options.ApprovalTimeField, options.SubsidiaryIds,
            oldestCutoff.Value, ct);
        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var result = await sender.Send(new ProcessAutoStrCmd(candidate.NetsuiteOrderId), ct);
                if (result.State is "Blocked" or "OutcomeUncertain" or "ConfigurationException")
                    logger.LogWarning("STR {OrderId} automation {State}: {Reason}", result.NetsuiteOrderId, result.State, result.Reason);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "STR {OrderId} auto-fulfillment processing failed; will re-evaluate safely", candidate.NetsuiteOrderId);
            }
        }
    }
}
