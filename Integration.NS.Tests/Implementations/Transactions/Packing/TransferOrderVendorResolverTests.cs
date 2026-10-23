using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions.Packing;

/// <summary>
/// Name-matching rules for the vendor barcode printed under the STR's Vendor
/// field. Field-confirmed on 2026-10-21: the barcode payload is the vendor NAME
/// (<c>ISABELA GIOS COMMERCIAL, INC.</c>), not a NetSuite internal id, so the name
/// is resolved against the vendors actually present on the transfer order and the
/// id is read from that data afterwards.
/// <para>
/// Candidates now arrive as ids only from a dedicated document-scoped query, so
/// they are independent of any vendor's row position in the line read. The names
/// a scan can match come from the vendor record — <c>entityid</c>,
/// <c>companyname</c>, <c>fullname</c>. The line's display text is deliberately no
/// longer a hint (design D4a).
/// </para>
/// </summary>
public class TransferOrderVendorResolverTests
{
    private const string OrderNumber = "ISTR-THM00100000776-2";
    private const string Scanned = "ISABELA GIOS COMMERCIAL, INC.";

    private static List<VendorLookupDTO> Candidates(params int[] ids) =>
        ids.Select(id => new VendorLookupDTO { Id = id }).ToList();

    private static VendorDTO Vendor(int id, string entityId, string companyName, string fullName)
        => new()
        {
            Id = id,
            ReferenceNumber = entityId,
            CompanyName = companyName,
            Name = fullName
        };

    private static List<VendorDTO> DocumentVendors() =>
    [
        Vendor(
            111,
            "ISABELA GIOS COMMERCIAL, INC.",
            "ISABELA GIOS COMMERCIAL INC.",
            "ISABELA GIOS COMMERCIAL, INC."),
        Vendor(
            222,
            "PHILIPPINE SPRING WATER RESOURCES INC.",
            "PHILIPPINE SPRING WATER RESOURCES INC.",
            "PHILIPPINE SPRING WATER RESOURCES INC.")
    ];

    [Fact]
    public void Vendor_options_only_include_named_vendors_from_the_document_candidates()
    {
        var options = TransferOrderVendorResolver.GetOptions(
            Candidates(111, 222, 333),
            [
                .. DocumentVendors(),
                Vendor(444, "OTHER VENDOR", "OTHER VENDOR", "OTHER VENDOR"),
                Vendor(333, "", "", "")
            ]);

        Assert.Equal(new[] { 111, 222 }, options.Select(option => option.Id).OrderBy(id => id));
        Assert.All(options, option => Assert.False(string.IsNullOrWhiteSpace(option.Name)));
    }

    [Fact]
    public void Scanned_name_resolves_to_that_vendors_id()
    {
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111, 222), DocumentVendors(), Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
        Assert.Equal(Scanned, result.Data.Name);
    }

    [Fact]
    public void Matching_ignores_case_and_extra_whitespace()
    {
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111, 222), DocumentVendors(), "  isabela   gios commercial, inc.  ", OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Matching_falls_back_to_ignoring_punctuation()
    {
        // Printed labels and record names can disagree by a comma or a period.
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111, 222), DocumentVendors(), "ISABELA GIOS COMMERCIAL INC", OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    // ---------------------------------------------------------------------
    // Field regression (2026-10-21): the name printed on the paper did not equal
    // the value the line carried, so the scan resolved to nothing even though the
    // vendor is on the order. The vendor record's own names close that gap.
    // ---------------------------------------------------------------------
    [Fact]
    public void Resolves_from_the_vendor_record_when_the_line_display_name_differs()
    {
        var candidates = Candidates(111);
        var vendors = new[]
        {
            Vendor(
                111,
                "ISABELA GIOS COM. INC.",
                "ISABELA GIOS COMMERCIAL INC.",
                "ISABELA GIOS COM. INC. : ISABELA GIOS COMMERCIAL INC.")
        };

        var result = TransferOrderVendorResolver.Resolve(candidates, vendors, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
        Assert.Equal("ISABELA GIOS COMMERCIAL INC.", result.Data.Name);
    }

    [Fact]
    public void Resolves_from_the_vendor_entity_id()
    {
        var vendors = new[] { Vendor(111, Scanned, "SOMETHING ELSE LLC", "SOMETHING ELSE LLC") };

        var result = TransferOrderVendorResolver.Resolve(Candidates(111), vendors, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Resolves_from_the_vendor_full_name()
    {
        var vendors = new[] { Vendor(111, "IGCI", "SOMETHING ELSE LLC", Scanned) };

        var result = TransferOrderVendorResolver.Resolve(Candidates(111), vendors, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Name_that_is_not_on_the_document_is_rejected_with_the_scan_and_the_candidates()
    {
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111, 222), DocumentVendors(), "SOME OTHER VENDOR, INC.", OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("SOME OTHER VENDOR, INC.", result.ErrorMessage);
        Assert.Contains(OrderNumber, result.ErrorMessage);
        Assert.Contains("Vendors on this order", result.ErrorMessage);
    }

    [Fact]
    public void Two_vendors_that_only_differ_by_punctuation_are_reported_as_ambiguous()
    {
        var vendors = new[]
        {
            Vendor(111, "ABC CO., LTD", "ABC CO., LTD", "ABC CO., LTD"),
            Vendor(222, "ABC CO. LTD", "ABC CO. LTD", "ABC CO. LTD")
        };

        // Strict: no exact match. Loose: both collapse to "ABC CO LTD".
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111, 222), vendors, "ABC CO, LTD", OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("more than one vendor", result.ErrorMessage);
    }

    [Fact]
    public void Blank_scan_asks_for_the_vendor_barcode()
    {
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111), DocumentVendors(), "   ", OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("Scan the vendor barcode", result.ErrorMessage);
    }

    [Fact]
    public void Candidates_without_a_vendor_are_ignored()
    {
        // A SHIPPING line with no vendor produces no candidate row.
        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111), [.. DocumentVendors().Take(1)], Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Order_with_no_vendor_on_any_line_is_reported_clearly()
    {
        var result = TransferOrderVendorResolver.Resolve([], [], Scanned, OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("has no vendor on its lines", result.ErrorMessage);
        Assert.Contains("web Packing", result.ErrorMessage);
    }

    [Fact]
    public void Vendor_ids_present_but_no_name_anywhere_is_reported_clearly()
    {
        var result = TransferOrderVendorResolver.Resolve(Candidates(111), null, Scanned, OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("could not be read", result.ErrorMessage);
    }

    /// <summary>
    /// The candidate query is distinct over the whole document, so a vendor whose
    /// lines sit beyond the line read's first 1,000 rows is still a candidate.
    /// </summary>
    [Fact]
    public void Vendor_beyond_the_first_thousand_lines_still_resolves()
    {
        const int BeyondFirstPage = 9999;
        var vendors = new[] { Vendor(BeyondFirstPage, "LATE VENDOR, INC.", "LATE VENDOR INC.", "LATE VENDOR, INC.") };

        var result = TransferOrderVendorResolver.Resolve(
            Candidates(111, BeyondFirstPage), vendors, "LATE VENDOR, INC.", OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(BeyondFirstPage, result.Data!.Id);
    }
}
