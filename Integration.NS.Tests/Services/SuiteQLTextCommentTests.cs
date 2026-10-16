using System.Text.RegularExpressions;
using Integration.NS.Helpers;
using Integration.NS.Tests.Asserts;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Coverage for <c>SuiteQLText.StripComments</c>, the guard that lets a stored
/// script keep its comments without them reaching NetSuite.
/// <para>
/// The defect this locks: every SuiteQL statement is flattened with
/// <c>Regex.Replace(query, @"\s+", " ")</c> before it is sent, so a <c>--</c>
/// line comment in a multi-line script becomes a comment running to the end of
/// the entire statement. The remaining joins, the WHERE clause and the closing
/// parenthesis of the derived table are silently discarded, and NetSuite answers
/// 400 INVALID_PARAMETER with "end of input ... no_root_node" — an error that
/// names no offending token. NS_TO_x_Packing_Get_Items,
/// NS_TO_x_Return_x_Packing_Get_Items and NS_VendorReturnAuthorization_Get_Items
/// all shipped in that state.
/// </para>
/// </summary>
public partial class SuiteQLTextCommentTests
{
    /// <summary>Mirrors NetSuiteApiClientService.FormatQuery after the fix.</summary>
    private static string FormatQuery(string query)
        => Regex.Replace(SuiteQLText.StripComments(query), @"\s+", " ").Trim();

    [Fact]
    public void Line_comment_is_removed_and_the_next_line_survives()
    {
        const string script = "SELECT a\n-- this whole line goes\nFROM t";

        var result = SuiteQLText.StripComments(script);

        Assert.Equal("SELECT a\n\nFROM t", result);
        Assert.DoesNotContain("this whole line", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Comment_marker_inside_a_string_literal_is_preserved()
    {
        const string script = "SELECT TO_CHAR(d, 'YYYY--MM') FROM t";

        var result = SuiteQLText.StripComments(script);

        Assert.Equal(script, result);
    }

    [Fact]
    public void Doubled_quote_does_not_close_the_literal()
    {
        // 'O''Reilly' is one literal, not two. Treating the second quote as a
        // terminator would put the scanner outside the literal and truncate the
        // rest of the statement at any -- that followed.
        const string script = "SELECT 1 FROM t WHERE n = 'O''Reilly' -- gone\nAND id = 2";

        var result = SuiteQLText.StripComments(script);

        Assert.Contains("'O''Reilly'", result, StringComparison.Ordinal);
        Assert.DoesNotContain("gone", result, StringComparison.Ordinal);
        Assert.Contains("AND id = 2", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Block_comment_is_removed()
    {
        const string script = "SELECT /* inline note */ a FROM t";

        var result = SuiteQLText.StripComments(script);

        Assert.Equal("SELECT  a FROM t", result);
    }

    [Fact]
    public void Unterminated_block_comment_consumes_the_remainder()
    {
        var result = SuiteQLText.StripComments("SELECT a FROM t /* never closed");

        Assert.Equal("SELECT a FROM t ", result);
    }

    [Fact]
    public void Query_without_comment_markers_is_returned_unchanged()
    {
        const string script = "SELECT a FROM t WHERE b = 'c'";

        Assert.Same(script, SuiteQLText.StripComments(script));
    }

    /// <summary>
    /// The production shape: a wrapped script whose WHERE clause and closing
    /// parenthesis sit after a comment. Before the fix the flattened query ended
    /// at the comment, which is what produced "end of input".
    /// </summary>
    [Fact]
    public void Flattened_query_keeps_its_where_clause_and_closing_paren()
    {
        var script = """
            SELECT
                t.id AS OrderId
            FROM item i
            JOIN transactionline tl ON i.id = tl.item
            -- one row per (item, location) via GROUP BY on a MIN()
            LEFT JOIN bin b ON b.id = tl.item
            WHERE tl.transactionlinetype = 'SHIPPING'
            """.ReplaceLineEndings("\n");

        var flattened = FormatQuery(script);

        Assert.Contains("WHERE tl.transactionlinetype = 'SHIPPING'", flattened, StringComparison.Ordinal);
        SuiteQLAssert.WellFormed(flattened);
    }

    /// <summary>
    /// Repos-wide guard. The three scripts below shipped broken precisely because
    /// they were the only files in NSScripts carrying comments; a new one must
    /// not reintroduce the class. Their rationale now lives in the C# that
    /// consumes them.
    /// </summary>
    [Fact]
    public void No_shipping_script_carries_a_sql_comment()
    {
        var scriptsDirectory = LocateNSScriptsDirectory();
        var offenders = Directory
            .GetFiles(scriptsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .Where(file => SuiteQLAssert.HasComment(File.ReadAllText(file)))
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"script(s) [{string.Join(", ", offenders)}] contain a SQL comment. Comments are stripped " +
            "before transport, but a comment in a shipped script means its rationale will be silently " +
            "dropped the next time the stripper changes. Move the explanation to the consuming C#.");
    }

    /// <summary>
    /// Every script still has to be structurally sound after stripping, so a
    /// stray block comment cannot unbalance parentheses on its way out.
    /// </summary>
    [Fact]
    public void Every_shipping_script_is_well_formed_after_stripping()
    {
        var scriptsDirectory = LocateNSScriptsDirectory();

        foreach (var file in Directory.GetFiles(scriptsDirectory, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var flattened = FormatQuery(SubstitutePlaceholders(File.ReadAllText(file)));

            var failure = Record.Exception(() => SuiteQLAssert.WellFormed(flattened));
            Assert.True(
                failure is null,
                $"script '{Path.GetFileName(file)}' is not well formed after flattening: {failure?.Message}");
        }
    }

    /// <summary>
    /// Fills every script parameter with a shape-valid value so the shared
    /// invariants can run on an unresolved script: <c>@name</c> becomes a quoted
    /// literal, <c>{name}</c> becomes a single integer (never an empty IN list).
    /// </summary>
    private static string SubstitutePlaceholders(string script)
        => Placeholder()
            .Replace(script, m => m.Value.StartsWith('@') ? "'X'" : "1");

    [GeneratedRegex(@"@[a-zA-Z_]\w*|\{[a-zA-Z_]\w*\}")]
    private static partial Regex Placeholder();

    private static string LocateNSScriptsDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Integration.NS", "NSScripts");
            if (Directory.Exists(candidate)) return candidate;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Integration.NS/NSScripts walking up from '{AppContext.BaseDirectory}'.");
    }
}