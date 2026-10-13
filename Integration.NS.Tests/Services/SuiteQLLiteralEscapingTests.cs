using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Regression coverage for string-literal escaping in SuiteQL filters.
///
/// Bug: filter values (doc tranids, user-typed grid Contains filters) were
/// interpolated raw, so a value containing an apostrophe terminated the string
/// literal early and NetSuite rejected the whole query with
/// 400 [INVALID_PARAMETER] — the same error family as the correlated-subquery
/// incidents. Every string value that reaches a WHERE clause or LIKE pattern
/// must have its single quotes doubled.
/// </summary>
public class SuiteQLLiteralEscapingTests
{
    private static SuiteQLQueryBuilderFactoryService Factory => new();

    [Fact]
    public void Equals_WithApostrophe_DoublesTheQuote()
    {
        var query = Factory.Create()
            .Select(("t.tranid", "Ref"))
            .From("transaction t")
            .WithFilters(DataGridFilterUtilities.Equal("t.tranid", "PO'123"))
            .Build();

        Assert.Contains("t.tranid = 'PO''123'", query.Query);
    }

    [Fact]
    public void In_List_EachesElementIsEscaped()
    {
        var query = Factory.Create()
            .Select(("t.tranid", "Ref"))
            .From("transaction t")
            .WithFilters(DataGridFilterUtilities.In("t.tranid", new[] { "PO'1", "PO2" }))
            .Build();

        Assert.Contains("t.tranid IN ('PO''1', 'PO2')", query.Query);
    }

    [Theory]
    [InlineData(ComparisonOperatorEnum.Contains, "LOWER(t.memo) LIKE LOWER('%John''s order%')")]
    [InlineData(ComparisonOperatorEnum.StartsWith, "LOWER(t.memo) LIKE LOWER('John''s order%')")]
    [InlineData(ComparisonOperatorEnum.EndsWith, "LOWER(t.memo) LIKE LOWER('%John''s order')")]
    public void String_Filters_EscapeThePattern(ComparisonOperatorEnum op, string expected)
    {
        var query = Factory.Create()
            .Select(("t.memo", "Memo"))
            .From("transaction t")
            .WithFilters(DataGridFilterUtilities.Filter("t.memo", op, "John's order"))
            .Build();

        Assert.Contains(expected, query.Query);
    }

    [Fact]
    public void Values_WithoutApostrophes_AreUnchanged()
    {
        var query = Factory.Create()
            .Select(("t.tranid", "Ref"))
            .From("transaction t")
            .WithFilters(DataGridFilterUtilities.Equal("t.tranid", "IF-THM00100000660"))
            .Build();

        Assert.Contains("t.tranid = 'IF-THM00100000660'", query.Query);
    }
}
