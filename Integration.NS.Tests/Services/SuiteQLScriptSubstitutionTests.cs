using Integration.NS.Helpers;
using Integration.NS.Tests.Asserts;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Input-class matrix over the single SuiteQL script-substitution chokepoint
/// (<c>SuiteQLText</c>), which both <c>NetSuiteApiClientService.NetsuiteQuery</c>
/// and <c>SuiteQLScriptResolver.ResolveSuiteQLScript</c> now delegate to. Testing
/// this one function protects all ~27 named-script call sites.
///
/// Background (2026-10-13 review): the scanned-barcode vector proposed in an
/// earlier analysis does not exist — NS_Item_Get_PerBarcode.sql uses @barcode,
/// the escaped form. The real risks on the {key} raw-splice form are unescaped
/// text and empty selections producing 'IN ()' (both now guarded here), and the
/// @key form is asserted escaped for every input class.
/// </summary>
public class SuiteQLScriptSubstitutionTests
{
    [Theory]
    [InlineData("ABC123", "'ABC123'")]                  // the 99% case
    [InlineData("O'Reilly", "'O''Reilly'")]            // apostrophe — the defect class
    [InlineData("'; DROP TABLE --", "'''; DROP TABLE --'")] // injection attempt, neutralized
    [InlineData("", "''")]                              // empty
    [InlineData("áéí", "'áéí'")]                        // unicode
    [InlineData("000123", "'000123'")]                  // leading zeros preserved as text
    public void At_parameter_is_always_spliced_as_an_escaped_literal(string value, string expected)
    {
        var query = SuiteQLText.SubstituteScriptParameters(
            "bc.name = @barcode",
            new Dictionary<string, string> { ["barcode"] = value });

        Assert.Contains($"bc.name = {expected}", query);
        SuiteQLAssert.WellFormed(query); // even the injection class yields closed literals
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("1,2,3", "1,2,3")]
    [InlineData("5, 7", "5, 7")]                        // internal whitespace tolerated
    [InlineData(" 12 ", "12")]                          // surrounding whitespace trimmed
    public void Bare_parameter_splices_a_valid_integer_list(string value, string expectedSplice)
    {
        var query = SuiteQLText.SubstituteScriptParameters(
            "b.location IN ({locations})",
            new Dictionary<string, string> { ["locations"] = value });

        Assert.Contains($"b.location IN ({expectedSplice})", query);
        SuiteQLAssert.WellFormed(query);
    }

    [Theory]
    [InlineData("")]                       // empty selection → 'IN ()' → 400 on NetSuite
    [InlineData("   ")]                    // whitespace-only
    [InlineData("1; DROP TABLE --")]       // injection
    [InlineData("O'Reilly")]               // unescaped text would break the statement
    [InlineData("1,2,x")]                  // non-integer element
    [InlineData("1,,3")]                   // empty element
    [InlineData("-")]                      // garbage
    public void Bare_parameter_rejects_everything_that_is_not_a_nonempty_integer_list(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SuiteQLText.SubstituteScriptParameters(
                "b.location IN ({locations})",
                new Dictionary<string, string> { ["locations"] = value }));

        Assert.Contains("{locations}", exception.Message);
    }

    [Fact]
    public void Bare_parameter_validation_never_fires_when_the_token_is_absent()
    {
        // Regression guard: scripts that only use @key for free text (e.g. a
        // scanned barcode) must not trip the {key} integer-list guard.
        var query = SuiteQLText.SubstituteScriptParameters(
            "bc.name = @barcode",
            new Dictionary<string, string> { ["barcode"] = "O'Reilly" });

        Assert.Contains("bc.name = 'O''Reilly'", query);
    }

    [Fact]
    public void Both_forms_in_one_script_are_substituted_independently()
    {
        var query = SuiteQLText.SubstituteScriptParameters(
            "t.tranid = @tranid AND i.id IN ({items})",
            new Dictionary<string, string>
            {
                ["tranid"] = "STR'1",
                ["items"] = "7,9"
            });

        Assert.Contains("t.tranid = 'STR''1'", query);
        Assert.Contains("i.id IN (7,9)", query);
        SuiteQLAssert.WellFormed(query);
    }

    [Fact]
    public void Missing_parameters_leave_the_script_unchanged()
    {
        const string script = "SELECT 1 FROM transaction t WHERE t.tranid = @tranid";

        var query = SuiteQLText.SubstituteScriptParameters(script, null);

        Assert.Equal(script, query);
        // An unsubstituted parameter would be caught by WellFormed; asserting
        // equality above is the contract here.
        var exception = Assert.Throws<Xunit.Sdk.TrueException>(() => SuiteQLAssert.WellFormed(query));
        Assert.Contains("@tranid", exception.Message);
    }
}
