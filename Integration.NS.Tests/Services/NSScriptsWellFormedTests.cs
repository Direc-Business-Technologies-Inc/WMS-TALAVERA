using Integration.NS.Tests.Asserts;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Scans the REAL <c>Integration.NS/NSScripts/*.sql</c> files, not fakes, so a
/// structural defect added by a script edit fails here. The existing
/// <see cref="SuiteQLScriptResolverTests"/> substitute against fake scripts and
/// therefore never validated the shipped files — which is how the
/// <c>NS_Get_Locations</c> / <c>NS_Get_Helpers</c> trailing commas (400
/// INVALID_PARAMETER) shipped unnoticed.
/// <para>
/// Only the parameter-independent invariants run here; the substitution
/// invariants (empty <c>IN ()</c>, unsubstituted token) need representative
/// values and stay in the resolver tests.
/// </para>
/// </summary>
public class NSScriptsWellFormedTests
{
    /// <summary>
    /// Integration.NS copies <c>NSScripts/**</c> to <c>Scripts/</c> next to the
    /// consuming assembly, so the test host sees them at <c>&lt;base&gt;/Scripts</c>.
    /// Falls back to the source folder if the copy is not present.
    /// </summary>
    private static string ScriptsDirectory()
    {
        var copied = Path.Combine(AppContext.BaseDirectory, "Scripts");
        if (Directory.Exists(copied) && Directory.EnumerateFiles(copied, "*.sql").Any())
        {
            return copied;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Integration.NS", "NSScripts");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the NSScripts directory (Scripts/ output dir or Integration.NS/NSScripts).");
    }

    public static IEnumerable<object[]> ScriptFiles() =>
        Directory.EnumerateFiles(ScriptsDirectory(), "*.sql")
            .OrderBy(Path.GetFileName)
            .Select(path => new object[] { Path.GetFileName(path), path });

    [Theory]
    [MemberData(nameof(ScriptFiles))]
    public void Real_script_has_no_trailing_comma(string name, string path)
    {
        var sql = File.ReadAllText(path);

        Assert.False(string.IsNullOrWhiteSpace(sql), $"{name} is empty");
        SuiteQLAssert.NoTrailingComma(sql);
    }

    /// <summary>
    /// Guards against the enumeration silently returning nothing, which would
    /// make every <c>[Theory]</c> case vacuously green.
    /// </summary>
    [Fact]
    public void Script_scan_covers_the_named_scripts()
    {
        Assert.True(
            ScriptFiles().Count() >= 20,
            "expected the NSScripts set to be enumerated; found too few .sql files");
    }
}
