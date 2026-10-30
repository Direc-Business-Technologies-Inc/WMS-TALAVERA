namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

/// <summary>
/// A live NetSuite Fulfillment Exemption rule. IDs are NetSuite internal IDs;
/// automation flags remain raw until their Y/N list values are verified.
/// </summary>
public sealed record FulfillmentExemptionRuleDTO(
    string RuleId,
    string? TransferCategoryId,
    string? FromLocationId,
    string? ToLocationId,
    string? AutomatedPackingValue,
    string? AutomatedShippingValue,
    string? LabelTemplateId,
    bool IsInactive);

public enum FulfillmentExemptionDecision
{
    Skipped,
    ConfigurationException,
    Eligible
}

public sealed record FulfillmentExemptionEvaluation(
    FulfillmentExemptionDecision Decision,
    FulfillmentExemptionRuleDTO? Rule,
    string Reason);

/// <summary>
/// Fail-closed rule matching. The stored Y/N value is supplied by deployment
/// configuration only after it has been verified in NetSuite; it is never
/// inferred to be "T", "Y", or another value.
/// </summary>
public static class FulfillmentExemptionRuleEvaluator
{
    public static FulfillmentExemptionEvaluation Evaluate(
        IEnumerable<FulfillmentExemptionRuleDTO> rules,
        string? categoryId,
        string? fromLocationId,
        string? toLocationId,
        string? verifiedYesValue)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (string.IsNullOrWhiteSpace(categoryId) ||
            string.IsNullOrWhiteSpace(fromLocationId) ||
            string.IsNullOrWhiteSpace(toLocationId))
        {
            return new(FulfillmentExemptionDecision.ConfigurationException, null,
                "The STR category or location IDs are missing.");
        }

        var matches = rules.Where(rule => !rule.IsInactive &&
            string.Equals(rule.TransferCategoryId, categoryId, StringComparison.Ordinal) &&
            string.Equals(rule.FromLocationId, fromLocationId, StringComparison.Ordinal) &&
            string.Equals(rule.ToLocationId, toLocationId, StringComparison.Ordinal)).ToArray();

        if (matches.Length == 0)
            return new(FulfillmentExemptionDecision.Skipped, null, "No active exact rule match.");

        if (matches.Length != 1)
            return new(FulfillmentExemptionDecision.ConfigurationException, null,
                "Multiple active Fulfillment Exemption rules match this STR.");

        var match = matches[0];
        if (string.IsNullOrWhiteSpace(match.RuleId) ||
            string.IsNullOrWhiteSpace(match.AutomatedPackingValue) ||
            string.IsNullOrWhiteSpace(match.AutomatedShippingValue) ||
            string.IsNullOrWhiteSpace(match.LabelTemplateId))
        {
            return new(FulfillmentExemptionDecision.ConfigurationException, match,
                "The matching rule is missing a required value, including its label template.");
        }

        if (string.IsNullOrWhiteSpace(verifiedYesValue))
            return new(FulfillmentExemptionDecision.ConfigurationException, match,
                "The NetSuite Y/N Yes internal ID has not been verified; automation is disabled.");

        if (!string.Equals(match.AutomatedPackingValue, verifiedYesValue, StringComparison.Ordinal) ||
            !string.Equals(match.AutomatedShippingValue, verifiedYesValue, StringComparison.Ordinal))
        {
            return new(FulfillmentExemptionDecision.Skipped, match,
                "The matching rule is not configured Yes/Yes.");
        }

        return new(FulfillmentExemptionDecision.Eligible, match,
            "One active exact Yes/Yes rule matched.");
    }
}
