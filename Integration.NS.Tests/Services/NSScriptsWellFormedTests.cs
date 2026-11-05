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
    /// Rule 1: a derived table must not reference an outer alias. This shipped
    /// in <c>NS_InventoryCounting_Get_Items</c> on 2026-10-20 (four correlated
    /// scalar subqueries over <c>t.id</c>/<c>tl.id</c>), whose failure surfaced
    /// in mobile as a null-payload crash instead of a NetSuite error.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScriptFiles))]
    public void Real_script_has_no_outer_alias_reference_in_derived_table(string name, string path)
    {
        var sql = File.ReadAllText(path);

        Assert.False(string.IsNullOrWhiteSpace(sql), $"{name} is empty");
        SuiteQLAssert.NoOuterAliasReferenceInDerivedTable(sql);
    }

    /// <summary>
    /// 2026-10-20 regression: the counted-quantity columns were added as four
    /// scalar correlated subqueries. The mandate for this query is derived
    /// tables joined on their own output columns, so any scalar subquery is a
    /// re-introduction of the rejected shape.
    /// </summary>
    [Fact]
    public void Inventory_counting_items_script_has_no_scalar_subquery()
    {
        var sql = File.ReadAllText(FindScript("NS_InventoryCounting_Get_Items.sql"));

        Assert.DoesNotContain("(SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <c>inventorystatus</c> is text in SuiteQL — every production script
    /// compares it quoted. A bare numeric compare is a different predicate and
    /// must not slip back in.
    /// </summary>
    [Fact]
    public void Inventory_counting_items_script_quotes_inventory_status()
    {
        var sql = File.ReadAllText(FindScript("NS_InventoryCounting_Get_Items.sql"));

        Assert.DoesNotContain("inventorystatus = 1", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inventorystatus = 3", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inventorystatus = '1'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inventorystatus = '3'", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <c>transactionline.countquantity</c> is referenced by no other script and
    /// its suite-QL name was never confirmed against the sandbox. The mobile
    /// screen maps <c>CountedQuantity</c> but never reads it, so projecting it
    /// buys nothing and risks a 400.
    /// </summary>
    [Fact]
    public void Inventory_counting_items_script_does_not_project_countquantity()
    {
        var sql = File.ReadAllText(FindScript("NS_InventoryCounting_Get_Items.sql"));

        // 'COUNTQUANTITY' is also the transaction line type literal, so match
        // the column reference, not a bare substring.
        Assert.DoesNotContain(".countquantity", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The per-barcode lookup is the mobile scan resolver, so it must answer the
    /// same scope as the warm-list script (<c>NS_Item_Get_Barcodes</c>), which has
    /// never filtered by item type. The <c>itemtype = 'InvtPart'</c> filter this
    /// script used to carry silently made every non-InvtPart line unresolvable
    /// once the list was empty or stale.
    /// </summary>
    [Fact]
    public void Item_per_barcode_script_matches_the_barcode_list_scope()
    {
        var perBarcode = File.ReadAllText(FindScript("NS_Item_Get_PerBarcode.sql"));
        var list = File.ReadAllText(FindScript("NS_Item_Get_Barcodes.sql"));

        Assert.DoesNotContain("itemtype", perBarcode, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("itemtype", list, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bc.name = @barcode", perBarcode, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindScript(string fileName)
    {
        foreach (var row in ScriptFiles())
        {
            if (string.Equals((string)row[0], fileName, StringComparison.OrdinalIgnoreCase))
            {
                return (string)row[1];
            }
        }

        throw new FileNotFoundException($"script '{fileName}' not found in NSScripts");
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
