using Application.UseCases.Commands.Transaction.StockTransferRequest;
using Xunit;

namespace Integration.NS.Tests.Helpers;

public class StrAutoFulfillmentOptionsTests
{
    [Fact]
    public void Missing_contract_reports_actionable_environment_names()
    {
        var issues = new StrAutoFulfillmentOptions().GetReadinessIssues();
        Assert.Contains(issues, x => x.Contains("WMS_STR_AUTO_ENABLED"));
        Assert.Contains(issues, x => x.Contains("WMS_STR_AUTO_APPROVAL_TIME_FIELD"));
        Assert.Contains(issues, x => x.Contains("WMS_STR_AUTO_SUBSIDIARY_IDS"));
    }

    [Fact]
    public void Malformed_explicit_line_field_cannot_silently_use_category_fallback()
    {
        var options = new StrAutoFulfillmentOptions
        {
            Enabled = true, ApprovedStatusId = 4, YesListValueId = "1",
            ApprovalTimeField = "custbody_approved_at", ApprovalTimeIsUtcVerified = true,
            TemplateApplicationVerified = true, SubsidiaryIds = [1],
            CategoryStatusPolicyVerified = true, LineStatusField = "tl.custcol_condition"
        };
        Assert.False(options.Ready);
        Assert.Contains(options.GetReadinessIssues(), x => x.Contains("without a table prefix"));
    }

    [Fact]
    public void Account_settings_must_all_be_verified_before_worker_is_ready()
    {
        var options = new StrAutoFulfillmentOptions
        {
            Enabled = true,
            ApprovedStatusId = 9,
            YesListValueId = "2",
            ApprovalTimeField = "custbody_approved_at",
            ApprovalTimeIsUtcVerified = true,
            TemplateApplicationVerified = true,
            SubsidiaryIds = [1]
        };

        Assert.False(options.Ready); // no per-line condition source or approved category policy
        Assert.True(new StrAutoFulfillmentOptions
        {
            Enabled = true, ApprovedStatusId = 9, YesListValueId = "2",
            ApprovalTimeField = "custbody_approved_at", ApprovalTimeIsUtcVerified = true,
            TemplateApplicationVerified = true, SubsidiaryIds = [1], CategoryStatusPolicyVerified = true
        }.Ready);
        Assert.False(new StrAutoFulfillmentOptions
        {
            Enabled = true, ApprovedStatusId = 9, YesListValueId = "2",
            ApprovalTimeField = "custbody_approved_at", ApprovalTimeIsUtcVerified = true,
            TemplateApplicationVerified = true, SubsidiaryIds = [1],
            LineStatusField = "tl.custcol_condition"
        }.Ready);
        Assert.True(new StrAutoFulfillmentOptions
        {
            Enabled = true, ApprovedStatusId = 9, YesListValueId = "2",
            ApprovalTimeField = "custbody_approved_at", ApprovalTimeIsUtcVerified = true,
            TemplateApplicationVerified = true, SubsidiaryIds = [1],
            LineStatusField = "custcol_condition"
        }.Ready);
    }

    [Theory]
    [InlineData("custbody_approval_date", true)]
    [InlineData("t.custbody_approval_date", false)]
    [InlineData("custcol_condition", false)]
    [InlineData("custbody_x; DELETE", false)]
    public void Approval_field_is_validated_before_suiteql_interpolation(string? field, bool expected) =>
        Assert.Equal(expected, StrAutoFulfillmentOptions.IsApprovalTimeField(field));

    [Theory]
    [InlineData("custcol_condition", true)]
    [InlineData("custbody_condition", false)]
    [InlineData("tl.custcol_condition", false)]
    public void Line_status_field_is_validated_before_suiteql_interpolation(string? field, bool expected) =>
        Assert.Equal(expected, StrAutoFulfillmentOptions.IsLineStatusField(field));
}
