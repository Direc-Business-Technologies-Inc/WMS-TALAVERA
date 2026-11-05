using Integration.NS.Tests.Asserts;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Pins the Rule 1 lint itself (see <c>.opencode/docs/SuiteQL-Shape-Rules.md</c>):
/// a derived table that reaches back to an outer alias is rejected by NetSuite
/// with 400 INVALID_PARAMETER, so the assertion must flag that shape and must
/// leave the proven shapes alone.
/// </summary>
public class SuiteQLDerivedTableCorrelationTests
{
    [Fact]
    public void Rejects_a_derived_table_that_references_the_driving_table()
    {
        const string rejected = """
            SELECT t.id, ia.total
            FROM transaction t
            LEFT JOIN (
                SELECT SUM(ia.quantity) AS total
                FROM inventoryassignment ia
                WHERE ia.transaction = t.id
            ) ia ON ia.total > 0
            """;

        var error = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SuiteQLAssert.NoOuterAliasReferenceInDerivedTable(rejected));

        Assert.Contains("outer alias 't'", error.Message);
    }

    [Fact]
    public void Rejects_a_nested_subquery_that_reaches_the_outer_uom_alias()
    {
        const string rejected = """
            SELECT tl.id
            FROM transactionline tl
            JOIN unitstypeuom uom ON tl.units = uom.internalid
            LEFT JOIN (
                SELECT SUM(rtl.quantity) AS received, rtl.transaction
                FROM transactionline rtl
                WHERE rtl.units = uom.internalid
                GROUP BY rtl.transaction
            ) r ON r.transaction = tl.transaction
            """;

        var error = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => SuiteQLAssert.NoOuterAliasReferenceInDerivedTable(rejected));

        Assert.Contains("outer alias 'uom'", error.Message);
    }

    [Fact]
    public void Accepts_the_proven_shape_join_on_the_derived_tables_own_columns()
    {
        const string accepted = """
            SELECT t.id, ig.good
            FROM transaction t
            JOIN transactionline tl ON tl.transaction = t.id
            LEFT JOIN (
                SELECT ia.transaction AS TransactionId,
                       SUM(ia.quantity) AS good
                FROM inventoryassignment ia
                WHERE ia.inventorystatus = '1'
                GROUP BY ia.transaction
            ) ig ON ig.TransactionId = t.id
            """;

        SuiteQLAssert.NoOuterAliasReferenceInDerivedTable(accepted);
    }

    [Fact]
    public void Accepts_a_single_level_scalar_subquery_in_the_select_list()
    {
        // Accepted by NetSuite in production:
        // NS_TO_Get_Itemfulfillment_Items.sql correlates on tl.transaction/tl.id.
        const string accepted = """
            SELECT tl.id,
                   (SELECT SUM(rtl.quantity)
                    FROM previoustransactionlinelink pttl
                    JOIN transactionline rtl ON pttl.nextline = rtl.id
                    WHERE pttl.previousdoc = tl.transaction
                      AND pttl.previousline = tl.id) AS received
            FROM transactionline tl
            """;

        SuiteQLAssert.NoOuterAliasReferenceInDerivedTable(accepted);
    }
}
