using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Xunit;

namespace Integration.NS.Tests.Helpers;

public class FulfillmentExemptionRuleEvaluatorTests
{
    private static FulfillmentExemptionRuleDTO Rule(
        string category = "1", string from = "10", string to = "20",
        string pack = "yes-list-id", string ship = "yes-list-id",
        string template = "501", bool inactive = false) =>
        new("99", category, from, to, pack, ship, template, inactive);

    [Fact]
    public void Exact_active_internal_id_match_with_verified_yes_is_eligible()
    {
        var result = FulfillmentExemptionRuleEvaluator.Evaluate([Rule()], "1", "10", "20", "yes-list-id");

        Assert.Equal(FulfillmentExemptionDecision.Eligible, result.Decision);
    }

    [Fact]
    public void Unmatched_or_mixed_flags_are_normal_skips()
    {
        Assert.Equal(FulfillmentExemptionDecision.Skipped,
            FulfillmentExemptionRuleEvaluator.Evaluate([Rule()], "1", "10", "21", "yes-list-id").Decision);
        Assert.Equal(FulfillmentExemptionDecision.Skipped,
            FulfillmentExemptionRuleEvaluator.Evaluate([Rule(ship: "no-list-id")], "1", "10", "20", "yes-list-id").Decision);
    }

    [Fact]
    public void Duplicate_or_unverified_and_incomplete_configuration_fails_closed()
    {
        Assert.Equal(FulfillmentExemptionDecision.ConfigurationException,
            FulfillmentExemptionRuleEvaluator.Evaluate([Rule(), Rule("1", "10", "20", template: "502")], "1", "10", "20", "yes-list-id").Decision);
        Assert.Equal(FulfillmentExemptionDecision.ConfigurationException,
            FulfillmentExemptionRuleEvaluator.Evaluate([Rule()], "1", "10", "20", null).Decision);
        Assert.Equal(FulfillmentExemptionDecision.ConfigurationException,
            FulfillmentExemptionRuleEvaluator.Evaluate([Rule(template: "")], "1", "10", "20", "yes-list-id").Decision);
    }

    [Fact]
    public void Inactive_rules_do_not_match()
    {
        Assert.Equal(FulfillmentExemptionDecision.Skipped,
            FulfillmentExemptionRuleEvaluator.Evaluate([Rule(inactive: true)], "1", "10", "20", "yes-list-id").Decision);
    }
}
