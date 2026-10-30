using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Domain.Transaction.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.Transaction.StockTransferRequest;

/// <summary>Unattended processor; NetSuite approval and the rule are re-read, never trusted from a poll snapshot.</summary>
public record ProcessAutoStrCmd(int NetsuiteOrderId) : IRequest<StrAutoFulfillmentResultDTO>;

public sealed class ProcessAutoStrCmdHandler(
    IAutoStrNetSuiteIntegration netsuite,
    IStockTransferRequestIntegration rules,
    IStrAutoFulfillmentStore store,
    StrAutoFulfillmentOptions options,
    ILogger<ProcessAutoStrCmdHandler> logger) : IRequestHandler<ProcessAutoStrCmd, StrAutoFulfillmentResultDTO>
{
    public async Task<StrAutoFulfillmentResultDTO> Handle(ProcessAutoStrCmd request, CancellationToken ct)
    {
        var id = request.NetsuiteOrderId;
        StrAutoFulfillmentResultDTO Result(string state, string reason) => new(id, state, reason);
        if (!options.Ready) return Result("Disabled", "The NetSuite automation contract has not been verified.");
        var order = await netsuite.GetCurrentOrderAsync(id, options.ApprovalTimeField, ct);
        if (order is null || order.ApprovalStatusId != options.ApprovedStatusId ||
            !options.SubsidiaryIds.Contains(order.SubsidiaryId))
            return await Skip("The STR is not an approved order in the configured subsidiaries.");

        var route = $"{order.TransferCategoryId}:{order.FromLocationId}:{order.ToLocationId}";
        var cutoff = await store.GetOrCreateRouteCutoffAsync(route, ct);
        if (order.ApprovedAtUtc <= cutoff)
            return await Skip("The STR predates automation for this route.");

        var initial = FulfillmentExemptionRuleEvaluator.Evaluate(await rules.GetActiveFulfillmentExemptionRules(),
            order.TransferCategoryId.ToString(), order.FromLocationId.ToString(),
            order.ToLocationId.ToString(), options.YesListValueId);
        if (initial.Decision != FulfillmentExemptionDecision.Eligible)
        {
            await store.RecordDecisionAsync(id, initial.Decision.ToString(), initial.Reason, ct);
            return Result(initial.Decision.ToString(), initial.Reason);
        }

        var owner = await store.TryAcquireAsync(id, ct);
        if (owner is null) return Result("Skipped", "The STR is already being posted or needs reconciliation.");
        try
        {
            if (!await store.TryStartAutoAsync(id, owner.Value, ct))
                return Result("Skipped", "Automatic fulfillment has already completed or needs reconciliation.");

            // Approval or the rule may have changed while the worker acquired the claim.
            order = await netsuite.GetCurrentOrderAsync(id, options.ApprovalTimeField, ct);
            if (order is null || order.ApprovalStatusId != options.ApprovedStatusId ||
                !options.SubsidiaryIds.Contains(order.SubsidiaryId) || order.ApprovedAtUtc <= cutoff ||
                $"{order.TransferCategoryId}:{order.FromLocationId}:{order.ToLocationId}" != route)
                return await Block("Approval or route changed before fulfillment.");
            var match = FulfillmentExemptionRuleEvaluator.Evaluate(await rules.GetActiveFulfillmentExemptionRules(),
                order.TransferCategoryId.ToString(), order.FromLocationId.ToString(),
                order.ToLocationId.ToString(), options.YesListValueId);
            if (match.Decision != FulfillmentExemptionDecision.Eligible)
                return await Block(match.Reason);

            var before = await netsuite.GetFulfillmentIdsAsync(id, ct);
            var (lines, openCount) = await netsuite.GetCurrentLinesAsync(order,
                options.LineStatusField, options.CategoryStatusPolicyVerified, ct);
            var preflight = AutoFulfillmentPreflight.Check(lines.Select(x => x.Stock).ToArray(), openCount);
            if (preflight.Decision == AutoFulfillmentPreflightDecision.Blocked)
                return await Block(preflight.Reason);
            if (preflight.Decision == AutoFulfillmentPreflightDecision.NoOpenLines)
            {
                await store.RecordAutoOutcomeAsync(id, owner.Value, "Completed", "No open quantities remain.",
                    string.Join(',', before), ct);
                return Result("Completed", "No open quantities remain; nothing was posted.");
            }

            foreach (var line in lines.Where(x => preflight.RemainingBaseQuantityByLine.ContainsKey(x.Stock.LineId)))
                line.Posting.ScannedQuantity = preflight.RemainingBaseQuantityByLine[line.Stock.LineId] / line.Stock.UomRate;

            // Persist uncertainty before crossing the remote write boundary: a
            // crash after the POST must not allow another automatic attempt.
            await store.RecordAutoOutcomeAsync(id, owner.Value, "Posting", "NetSuite IF POST may be in progress.",
                string.Join(',', before), ct);
            try
            {
                await netsuite.PostShippedAsync(lines.Where(x => x.Posting.ScannedQuantity > 0).ToArray(), ct);
                var after = await netsuite.GetFulfillmentIdsAsync(id, ct);
                if (!after.Except(before).Any())
                    throw new InvalidOperationException("The NetSuite IF could not be verified after posting.");
                await store.RecordAutoOutcomeAsync(id, owner.Value, "Completed", "Direct-to-Shipped IF verified.",
                    string.Join(',', after), ct);
                return Result("Completed", "Direct-to-Shipped item fulfillment verified.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "STR {OrderId} IF POST or verification was uncertain", id);
                await store.RecordAutoOutcomeAsync(id, owner.Value, "OutcomeUncertain", ex.Message,
                    string.Join(',', before), CancellationToken.None);
                return Result("OutcomeUncertain", "Check NetSuite fulfillments before retrying.");
            }
        }
        finally
        {
            await store.ReleaseAsync(id, owner.Value, CancellationToken.None);
        }

        async Task<StrAutoFulfillmentResultDTO> Block(string reason)
        {
            await store.RecordAutoOutcomeAsync(id, owner!.Value, "Blocked", reason, null, ct);
            return Result("Blocked", reason);
        }

        async Task<StrAutoFulfillmentResultDTO> Skip(string reason)
        {
            await store.RecordDecisionAsync(id, "Skipped", reason, ct);
            return Result("Skipped", reason);
        }
    }
}
