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

    [GeneratedRegex(@"^\d+(\s*,\s*\d+)*$")]
    private static partial Regex IntegerList();
}
