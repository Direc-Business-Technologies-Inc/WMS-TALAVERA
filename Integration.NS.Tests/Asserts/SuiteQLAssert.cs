using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Integration.NS.Tests.Asserts;

/// <summary>
/// Shared invariant assertions for every SuiteQL string the WMS builds — from
/// either chokepoint (SuiteQLQueryBuilder output or named-script substitution).
/// One call proves a query is structurally well-formed before it is shipped;
/// it cannot prove NetSuite will accept it semantically (only a sandbox can —
/// deferred until credentials exist; see .opencode/docs/SuiteQL-Shape-Rules.md).
/// </summary>
public static partial class SuiteQLAssert
{
    /// <summary>
    /// Runs every invariant. Call sites: characterization tests over captured
    /// builder queries, and substitution tests over resolved scripts.
    /// </summary>
    public static void WellFormed(string query, int? limit = null, int? offset = null)
    {
        var (outsideLiterals, literalsClosed) = StripLiterals(query);

        Assert.True(literalsClosed, UnbalancedLiteralMessage(query));
        Assert.True(BalancedParens(outsideLiterals), $"unbalanced parentheses. Query: {query}");
        Assert.False(EmptyInList().IsMatch(outsideLiterals), $"empty IN () list. Query: {query}");
        AssertNoUnsubstitutedParameters(outsideLiterals, query);
        AssertNoTrailingComma(outsideLiterals, query);
        LimitBounds(limit);
        OffsetAligned(limit, offset);
    }

    /// <summary>
    /// Standalone structural check for a raw script (before parameter
    /// substitution), so a script can be validated without supplying values for
    /// its parameters. Runs only the trailing-comma invariant — the other
    /// invariants need substituted values and belong to <see cref="WellFormed"/>.
    /// </summary>
    public static void NoTrailingComma(string query)
    {
        var (outsideLiterals, _) = StripLiterals(query);
        AssertNoTrailingComma(outsideLiterals, query);
    }

    /// <summary>Convenience overload for a builder-produced query.</summary>
    public static void WellFormed(Application.DataTransferObjects.Others.NS.SuiteQLQuery query)
        => WellFormed(query.Query, query.Limit, query.Offset);

    /// <summary>
    /// True when the query carries a SQL comment (<c>--</c> or <c>/*</c>) outside
    /// a string literal. Comments are stripped before transport
    /// (<c>SuiteQLText.StripComments</c>), so this is a shape warning for stored
    /// scripts rather than a runtime failure — a comment in a shipped .sql means
    /// the rationale is one refactor away from being silently dropped. Literals
    /// are excluded, so a TO_CHAR format or a barcode value may still contain
    /// these characters safely.
    /// </summary>
    public static bool HasComment(string query)
    {
        var (outsideLiterals, _) = StripLiterals(query);
        return outsideLiterals.Contains("--", StringComparison.Ordinal)
            || outsideLiterals.Contains("/*", StringComparison.Ordinal);
    }

    /// <summary>
    /// Limits must be omitted (null → NetSuite default page) or within
    /// 1..1000 (Rule 3 of .opencode/docs/SuiteQL-Shape-Rules.md).
    /// </summary>
    public static void LimitBounds(int? limit)
    {
        if (limit is null) return;
        Assert.InRange(limit.Value, 1, 1000);
    }

    /// <summary>
    /// Offsets must be non-negative and, when a limit is present, divisible by
    /// it (Rule 4). Offset without limit relies on the stitched paging path.
    /// </summary>
    public static void OffsetAligned(int? limit, int? offset)
    {
        if (offset is null) return;
        Assert.True(offset.Value >= 0, $"negative offset {offset}");
        if (limit is not null)
        {
            Assert.True(offset.Value % limit.Value == 0,
                $"offset {offset} is not divisible by limit {limit}; NetSuite rejects that combination");
        }
    }

    /// <summary>
    /// Stateful scan over string literals: toggles in/out on a quote, but
    /// consumes a doubled quote ('') as an escape that stays inside. A query
    /// whose literal never closes — the unescaped-apostrophe defect class —
    /// reports closed=false.
    /// </summary>
    private static (string Outside, bool Closed) StripLiterals(string query)
    {
        var outside = new StringBuilder(query.Length);
        var inLiteral = false;

        for (var i = 0; i < query.Length; i++)
        {
            var ch = query[i];
            if (ch != '\'')
            {
                if (!inLiteral) outside.Append(ch);
                continue;
            }

            if (inLiteral && i + 1 < query.Length && query[i + 1] == '\'')
            {
                i++; // doubled quote inside a literal — consume both
                continue;
            }

            if (!inLiteral)
            {
                // Opening quote: emit a sentinel so a literal argument does not
                // collapse into a bare ", )" (e.g. TO_CHAR(x, 'fmt') -> "x, )")
                // and trip the structural checks.
                outside.Append('_');
            }

            inLiteral = !inLiteral;
        }

        return (outside.ToString(), !inLiteral);
    }

    private static bool BalancedParens(string text)
    {
        var depth = 0;
        foreach (var ch in text)
        {
            if (ch == '(') depth++;
            else if (ch == ')')
            {
                depth--;
                if (depth < 0) return false;
            }
        }

        return depth == 0;
    }

    private static void AssertNoUnsubstitutedParameters(string outsideLiterals, string query)
    {
        // Literal contents are stripped first, so data containing '@' or '{'
        // inside a quoted value can never trip this.
        var matches = UnsubstitutedParameter().Matches(outsideLiterals);
        Assert.True(matches.Count == 0,
            $"unsubstituted script parameter(s) [{string.Join(", ", matches.Select(m => m.Value))}] reached the query. " +
            $"Query: {query}");
    }

    private static void AssertNoTrailingComma(string outsideLiterals, string query)
    {
        // A comma immediately before a clause keyword or a closing paren is a
        // syntax error NetSuite answers with 400 INVALID_PARAMETER. This class of
        // defect shipped twice (NS_Get_Locations / NS_Get_Helpers) because every
        // other invariant passes on a query that still has a trailing comma.
        var match = TrailingComma().Match(outsideLiterals);
        Assert.True(!match.Success,
            $"trailing comma before '{match.Value.Trim()}' — NetSuite rejects the statement with 400 INVALID_PARAMETER. Query: {query}");
    }

    private static string UnbalancedLiteralMessage(string query)
    {
        var apostrophes = query.Count(c => c == '\'');
        return $"unbalanced string literal (apostrophe count {apostrophes}) — an unescaped apostrophe " +
               $"terminates the literal early and NetSuite rejects the query with 400 INVALID_PARAMETER. Query: {query}";
    }

    /// <summary>
    /// Rule 1 of .opencode/docs/SuiteQL-Shape-Rules.md: a derived table must
    /// never reference an alias of the outer query. NetSuite answers that with
    /// <c>400 INVALID_PARAMETER ... Invalid or unsupported search</c>.
    /// Correlation is allowed only in the JOIN's ON clause, against the derived
    /// table's own output columns.
    /// <para>
    /// Scope: subqueries in the FROM/JOIN clause only. A single-level scalar
    /// subquery in the SELECT list is accepted by NetSuite in production
    /// (<c>NS_TO_Get_Itemfulfillment_Items.sql</c> correlates on
    /// <c>tl.transaction</c>/<c>tl.id</c>), so flagging that class would force
    /// working queries to change. The recorded rejections are the nested
    /// (two-level) links and the derived-table shapes.
    /// </para>
    /// </summary>
    public static void NoOuterAliasReferenceInDerivedTable(string query)
    {
        var (outsideLiterals, _) = StripLiterals(query);
        var outerAliases = OuterAliases(outsideLiterals);

        if (outerAliases.Count == 0) return;

        foreach (var body in DerivedTableBodies(outsideLiterals))
        {
            var ownAliases = AliasesIntroduced(body);

            foreach (var alias in outerAliases)
            {
                if (ownAliases.Contains(alias)) continue;

                if (!Regex.IsMatch(body, $@"\b{Regex.Escape(alias)}\s*\.", RegexOptions.IgnoreCase)) continue;

                Assert.Fail(
                    $"derived table references outer alias '{alias}'. NetSuite rejects a correlated subquery/derived table " +
                    $"with 400 INVALID_PARAMETER (Rule 1). Resolve the correlation inside the derived table and join on its " +
                    $"output columns instead. Query: {query}");
            }
        }
    }

    /// <summary>
    /// Aliases bound at the top level of the statement (<c>FROM t</c>,
    /// <c>JOIN x y</c>). Text inside parentheses is skipped, so a derived
    /// table's own joins are not mistaken for outer aliases.
    /// </summary>
    private static HashSet<string> OuterAliases(string outsideLiterals)
    {
        var topLevel = new StringBuilder(outsideLiterals.Length);
        var depth = 0;

        foreach (var ch in outsideLiterals)
        {
            if (ch == '(') depth++;
            else if (ch == ')') depth--;
            else if (depth == 0) topLevel.Append(ch);
        }

        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Text.RegularExpressions.Match match in FromOrJoinAlias().Matches(topLevel.ToString()))
        {
            AddIfAlias(aliases, match.Groups["alias"].Value);
        }

        // Aliases of derived tables: "JOIN ( ... ) ig ON ..." — the parens are
        // stripped above, so match the closing paren in the original text.
        foreach (System.Text.RegularExpressions.Match match in DerivedTableAlias().Matches(outsideLiterals))
        {
            AddIfAlias(aliases, match.Groups["alias"].Value);
        }

        return aliases;
    }

    /// <summary>
    /// Bodies (without the wrapping parens) of every derived table —
    /// <c>FROM (SELECT ...)</c> / <c>JOIN (SELECT ...)</c>.
    /// </summary>
    private static IEnumerable<string> DerivedTableBodies(string outsideLiterals)
    {
        var starts = DerivedTableStart().Matches(outsideLiterals);

        foreach (System.Text.RegularExpressions.Match start in starts)
        {
            var open = start.Index + start.Value.IndexOf('(');
            var depth = 0;
            var close = -1;

            for (var i = open; i < outsideLiterals.Length; i++)
            {
                if (outsideLiterals[i] == '(') depth++;
                else if (outsideLiterals[i] == ')' && --depth == 0)
                {
                    close = i;
                    break;
                }
            }

            if (close < 0) continue;

            yield return outsideLiterals[(open + 1)..close];
        }
    }

    /// <summary>Aliases the subquery binds itself (its own FROM/JOIN).</summary>
    private static HashSet<string> AliasesIntroduced(string body)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in FromOrJoinAlias().Matches(body))
        {
            AddIfAlias(aliases, match.Groups["alias"].Value);
        }

        return aliases;
    }

    /// <summary>
    /// A table reference with no alias puts a clause keyword where the alias
    /// would be (<c>FROM AggregateItemLocation ORDER BY ...</c>). Keeping those
    /// out of the alias set stops the lint inventing "aliases" like ORDER.
    /// </summary>
    private static void AddIfAlias(HashSet<string> aliases, string candidate)
    {
        if (!SqlKeyword().IsMatch(candidate)) aliases.Add(candidate);
    }

    [GeneratedRegex(@"\b(?:FROM|JOIN)\s+(?!\()[A-Za-z_]\w*\s+(?:AS\s+)?(?<alias>[A-Za-z_]\w*)", RegexOptions.IgnoreCase)]
    private static partial Regex FromOrJoinAlias();

    [GeneratedRegex(@"\)\s+(?:AS\s+)?(?<alias>[A-Za-z_]\w*)\s+ON\b", RegexOptions.IgnoreCase)]
    private static partial Regex DerivedTableAlias();

    [GeneratedRegex(@"(?:FROM|JOIN)\s*\(\s*SELECT\b", RegexOptions.IgnoreCase)]
    private static partial Regex DerivedTableStart();

    [GeneratedRegex(@"^(?:AS|ON|WHERE|AND|OR|GROUP|ORDER|HAVING|LEFT|RIGHT|INNER|OUTER|FULL|JOIN|UNION|LIMIT|OFFSET)$", RegexOptions.IgnoreCase)]
    private static partial Regex SqlKeyword();

    [GeneratedRegex(@"IN\s*\(\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyInList();

    [GeneratedRegex(@"@[a-zA-Z_]\w*|\{[a-zA-Z_]\w*\}")]
    private static partial Regex UnsubstitutedParameter();

    [GeneratedRegex(@",\s*(?:FROM|WHERE|GROUP\s+BY|ORDER\s+BY|HAVING)\b|,\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingComma();
}
