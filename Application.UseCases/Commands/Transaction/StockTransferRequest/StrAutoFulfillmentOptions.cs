using System.Text.RegularExpressions;

namespace Application.UseCases.Commands.Transaction.StockTransferRequest;

/// <summary>
/// Account-specific values must come from a verified NetSuite sandbox contract.
/// Every switch defaults off; a saved rule alone cannot activate shipping.
/// Environment inputs: WMS_STR_AUTO_ENABLED, APPROVED_STATUS_ID,
/// YES_LIST_VALUE_ID, APPROVAL_TIME_FIELD, APPROVAL_TIME_IS_UTC_VERIFIED,
/// LINE_STATUS_FIELD or CATEGORY_STATUS_POLICY_VERIFIED,
/// TEMPLATE_APPLICATION_VERIFIED, SUBSIDIARY_IDS and POLL_SECONDS (all prefixed
/// WMS_STR_AUTO_). Keep these deployment-only and never commit account values.
/// </summary>
public sealed class StrAutoFulfillmentOptions
{
    public bool Enabled { get; init; }
    public int ApprovedStatusId { get; init; }
    public string YesListValueId { get; init; } = string.Empty;
    public string ApprovalTimeField { get; init; } = string.Empty;
    public string? LineStatusField { get; init; }
    public bool CategoryStatusPolicyVerified { get; init; }
    public bool ApprovalTimeIsUtcVerified { get; init; }
    public bool TemplateApplicationVerified { get; init; }
    public IReadOnlyList<int> SubsidiaryIds { get; init; } = [];
    public int PollSeconds { get; init; } = 120;

    public bool Ready => GetReadinessIssues().Count == 0;

    public IReadOnlyList<string> GetReadinessIssues()
    {
        var issues = new List<string>();
        if (!Enabled) issues.Add("WMS_STR_AUTO_ENABLED must be true.");
        if (ApprovedStatusId <= 0) issues.Add("WMS_STR_AUTO_APPROVED_STATUS_ID must be a positive internal ID.");
        if (string.IsNullOrWhiteSpace(YesListValueId)) issues.Add("WMS_STR_AUTO_YES_LIST_VALUE_ID is required.");
        if (!IsApprovalTimeField(ApprovalTimeField)) issues.Add("WMS_STR_AUTO_APPROVAL_TIME_FIELD must be an approval timestamp custbody_ field.");
        if (!ApprovalTimeIsUtcVerified) issues.Add("WMS_STR_AUTO_APPROVAL_TIME_IS_UTC_VERIFIED must be true after verifying SuiteQL timestamp timezone behavior.");
        if (!TemplateApplicationVerified) issues.Add("WMS_STR_AUTO_TEMPLATE_APPLICATION_VERIFIED must be true after verifying label template behavior.");
        if (!string.IsNullOrWhiteSpace(LineStatusField) && !IsLineStatusField(LineStatusField))
            issues.Add("WMS_STR_AUTO_LINE_STATUS_FIELD must be a custcol_ field without a table prefix.");
        if (!IsLineStatusField(LineStatusField) && !CategoryStatusPolicyVerified)
            issues.Add("Configure WMS_STR_AUTO_LINE_STATUS_FIELD or verify WMS_STR_AUTO_CATEGORY_STATUS_POLICY_VERIFIED.");
        if (SubsidiaryIds.Count == 0 || SubsidiaryIds.Any(id => id <= 0))
            issues.Add("WMS_STR_AUTO_SUBSIDIARY_IDS must contain positive subsidiary internal IDs.");
        return issues;
    }

    public static bool IsApprovalTimeField(string? field) =>
        !string.IsNullOrWhiteSpace(field) && Regex.IsMatch(field, @"^custbody_[a-z0-9_]+$", RegexOptions.CultureInvariant);

    public static bool IsLineStatusField(string? field) =>
        !string.IsNullOrWhiteSpace(field) && Regex.IsMatch(field, @"^custcol_[a-z0-9_]+$", RegexOptions.CultureInvariant);

    public static StrAutoFulfillmentOptions FromEnvironment()
    {
        static string? Read(string name) => Environment.GetEnvironmentVariable("WMS_STR_AUTO_" + name);
        static bool On(string name) => bool.TryParse(Read(name), out var value) && value;
        var subsidiaries = (Read("SUBSIDIARY_IDS") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0).ToArray();
        return new StrAutoFulfillmentOptions
        {
            Enabled = On("ENABLED"),
            ApprovedStatusId = int.TryParse(Read("APPROVED_STATUS_ID"), out var status) ? status : 0,
            YesListValueId = Read("YES_LIST_VALUE_ID") ?? string.Empty,
            ApprovalTimeField = Read("APPROVAL_TIME_FIELD") ?? string.Empty,
            LineStatusField = Read("LINE_STATUS_FIELD"),
            CategoryStatusPolicyVerified = On("CATEGORY_STATUS_POLICY_VERIFIED"),
            ApprovalTimeIsUtcVerified = On("APPROVAL_TIME_IS_UTC_VERIFIED"),
            TemplateApplicationVerified = On("TEMPLATE_APPLICATION_VERIFIED"),
            SubsidiaryIds = subsidiaries,
            PollSeconds = int.TryParse(Read("POLL_SECONDS"), out var seconds) && seconds is >= 30 and <= 3600
                ? seconds : 120
        };
    }
}
