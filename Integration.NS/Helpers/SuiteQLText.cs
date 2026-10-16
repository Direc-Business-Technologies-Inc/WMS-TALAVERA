using System.Text;
using System.Text.RegularExpressions;

namespace Integration.NS.Helpers;

/// <summary>
/// Single source for the SuiteQL script-substitution rules that used to be
/// duplicated (with diverging guarantees) in
/// <see cref="Services.NetSuiteApiClientService.NetsuiteQuery{T}"/> and
/// <c>SuiteQLScriptResolver.ResolveSuiteQLScript</c>.
/// <para>
/// Two parameter forms exist, and they carry different trust levels:
/// <list type="bullet">
/// <item><c>@key</c> — user/doc text (tranids, scanned barcodes, grid filter
/// input). Always spliced as a quoted, quote-escaped string literal.</item>
/// <item><c>{key}</c> — an identifier-list splice for <c>IN (...)</c>.
/// Because the value rides outside quotes, it is validated to be a non-empty,
/// comma-separated integer list before splicing; anything else throws locally
/// with a message naming the parameter instead of shipping broken SQL that
/// NetSuite answers with a 400 INVALID_PARAMETER. Do not introduce a {key}
/// splice of arbitrary text — pass it through @key instead.</item>
/// </list>
/// </para>
/// </summary>
internal static partial class SuiteQLText
{
    private static readonly Regex IntegerListPattern = IntegerList();

    /// <summary>Doubles single quotes so text can never terminate a string literal.</summary>
    public static string EscapeLiteral(string value) => value.Replace("'", "''");

    /// <summary>Wraps a value as a quoted, escaped SuiteQL string literal.</summary>
    public static string Literal(string raw) => $"'{EscapeLiteral(raw)}'";

    /// <summary>
    /// Substitutes the two parameter forms into a named SQL script. Semantics
    /// preserved from the pre-dedup behaviour: both forms are attempted for every
    /// parameter; @key replacement runs first; form-substitution is a no-op when
    /// the token is absent. The {key} validation fires only when the token is
    /// actually present, so scripts that use @key for non-list text keep working
    /// unchanged.
    /// </summary>
    public static string SubstituteScriptParameters(string query, IReadOnlyDictionary<string, string>? parameters)
    {
        if (parameters is null)
        {
            return query;
        }

        foreach (var parameter in parameters)
        {
            query = query.Replace($"@{parameter.Key}", Literal(parameter.Value));

            var rawToken = $"{{{parameter.Key}}}";
            if (query.Contains(rawToken))
            {
                query = query.Replace(rawToken, IntegerList(parameter.Key, parameter.Value));
            }
        }

        return query;
    }

    /// <summary>
    /// Guard for the {key} raw splice. Returns the value unchanged when it is a
    /// non-empty comma-separated integer list; otherwise throws before the query
    /// can reach NetSuite.
    /// </summary>
    public static string IntegerList(string key, string raw)
    {
        var candidate = raw.Trim();

        if (candidate.Length == 0)
        {
            throw new InvalidOperationException(
                $"SuiteQL parameter '{{{key}}}' received an empty list; the spliced 'IN ()' would be " +
                "rejected with a 400 INVALID_PARAMETER. Guard the caller against empty selections.");
        }

        if (!IntegerListPattern.IsMatch(candidate))
        {
            throw new InvalidOperationException(
                $"SuiteQL parameter '{{{key}}}' may only splice a comma-separated integer list into SQL " +
                $"(raw splices are outside string literals), but received: '{raw}'. " +
                "Pass text parameters through @key instead, which escapes them as string literals.");
        }

        return candidate;
    }

    /// <summary>
    /// Removes SQL comments (<c>--</c> to end of line, and <c>/* */</c> blocks)
    /// so a script survives <see cref="Services.NetSuiteApiClientService"/>'s
    /// whitespace collapse. Must run BEFORE newlines are folded into spaces.
    /// <para>
    /// Why this exists: every SuiteQL statement is flattened with
    /// <c>Regex.Replace(query, @"\s+", " ")</c> before it is sent. A line comment
    /// in a multi-line script therefore becomes a comment that runs to the end of
    /// the whole statement, silently discarding the remaining joins, the WHERE
    /// clause and the closing parenthesis of any derived table. NetSuite then
    /// answers 400 INVALID_PARAMETER with
    /// <c>"end of input ... no_root_node"</c>, which names no offending token and
    /// points at nothing. That is how the three packing line scripts shipped
    /// broken: they were the only scripts in NSScripts carrying <c>--</c>
    /// comments.
    /// </para>
    /// <para>
    /// The scan is string-literal aware, matching the literal handling in
    /// <c>SuiteQLAssert.StripLiterals</c>: a doubled quote (<c>''</c>) inside a
    /// literal is consumed as an escape and does not close it. Without this,
    /// data or a TO_CHAR format containing <c>--</c> would be truncated. Newlines
    /// terminating a line comment are preserved so the following statement is
    /// not joined onto the comment.
    /// </para>
    /// </summary>
    public static string StripComments(string query)
    {
        if (string.IsNullOrEmpty(query)) return query;
        if (!query.Contains("--", StringComparison.Ordinal) &&
            !query.Contains("/*", StringComparison.Ordinal)) return query;

        var stripped = new StringBuilder(query.Length);
        var inLiteral = false;

        for (var i = 0; i < query.Length; i++)
        {
            var ch = query[i];

            if (inLiteral)
            {
                stripped.Append(ch);
                if (ch == '\'')
                {
                    if (i + 1 < query.Length && query[i + 1] == '\'')
                    {
                        stripped.Append('\'');
                        i++; // doubled quote is an escape, not a terminator
                    }
                    else
                    {
                        inLiteral = false;
                    }
                }
                continue;
            }

            if (ch == '\'')
            {
                inLiteral = true;
                stripped.Append(ch);
                continue;
            }

            if (ch == '-' && i + 1 < query.Length && query[i + 1] == '-')
            {
                while (i < query.Length && query[i] != '\n') i++;
                if (i < query.Length) stripped.Append('\n');
                continue;
            }

            if (ch == '/' && i + 1 < query.Length && query[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < query.Length && !(query[i] == '*' && query[i + 1] == '/')) i++;
                i = Math.Min(i + 1, query.Length - 1);
                continue;
            }

            stripped.Append(ch);
        }

return stripped.ToString();
    }

    [GeneratedRegex(@"^\d+(\s*,\s*\d+)*$")]
    private static partial Regex IntegerList();
}
