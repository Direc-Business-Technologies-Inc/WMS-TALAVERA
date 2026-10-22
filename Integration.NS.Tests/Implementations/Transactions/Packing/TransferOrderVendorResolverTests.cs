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
/// The name a printed document carries is not necessarily the value the line query
/// projects: the print template may use the vendor's <c>companyname</c> while the
/// line carries <c>BUILTIN.DF</c>'s display text. Both the line name and the
/// record's <c>entityid</c> / <c>companyname</c> / <c>fullname</c> are therefore
/// candidates.
/// </para>
/// </summary>
public class TransferOrderVendorResolverTests
{
    private const string OrderNumber = "ISTR-THM00100000776-2";
    private const string Scanned = "ISABELA GIOS COMMERCIAL, INC.";

    private static TransferOrderLineDTO Line(int lineNumber, int? vendorId, string? vendorName)
        => new()
        {
            LineSequenceNumber = lineNumber,
            VendorId = vendorId,
            VendorName = vendorName
        };

    private static VendorDTO Vendor(int id, string entityId, string companyName, string fullName)
        => new()
        {
            Id = id,
            ReferenceNumber = entityId,
            CompanyName = companyName,
            Name = fullName
        };

    private static List<TransferOrderLineDTO> Lines() =>
    [
        Line(1, 111, "ISABELA GIOS COMMERCIAL, INC."),
        Line(2, 222, "PHILIPPINE SPRING WATER RESOURCES INC."),
        Line(3, 111, "ISABELA GIOS COMMERCIAL, INC."),
        Line(4, null, null)
    ];

    [Fact]
    public void Scanned_name_resolves_to_that_vendors_id()
    {
        var result = TransferOrderVendorResolver.Resolve(Lines(), null, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
        Assert.Equal(Scanned, result.Data.Name);
    }

    [Fact]
    public void Matching_ignores_case_and_extra_whitespace()
    {
        var result = TransferOrderVendorResolver.Resolve(
            Lines(), null, "  isabela   gios commercial, inc.  ", OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Matching_falls_back_to_ignoring_punctuation()
    {
        // Printed labels and BUILTIN.DF can disagree by a comma or a period.
        var result = TransferOrderVendorResolver.Resolve(
            Lines(), null, "ISABELA GIOS COMMERCIAL INC", OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    // ---------------------------------------------------------------------
    // Field regression (2026-10-21): the line's display name did not equal the
    // name printed on the paper, so the scan resolved to nothing even though the
    // vendor is on the order. The vendor record's own names close that gap.
    // ---------------------------------------------------------------------
    [Fact]
    public void Resolves_from_the_vendor_record_when_the_line_display_name_differs()
    {
        var lines = new List<TransferOrderLineDTO> { Line(1, 111, "ISABELA GIOS COM. INC.") };
        var vendors = new[]
        {
            Vendor(
                111,
                "ISABELA GIOS COM. INC.",
                "ISABELA GIOS COMMERCIAL INC.",
                "ISABELA GIOS COM. INC. : ISABELA GIOS COMMERCIAL INC.")
        };

        var result = TransferOrderVendorResolver.Resolve(lines, vendors, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
        Assert.Equal("ISABELA GIOS COMMERCIAL INC.", result.Data.Name);
    }

    [Fact]
    public void Resolves_from_the_vendor_entity_id()
    {
        var vendors = new[] { Vendor(111, Scanned, "SOMETHING ELSE LLC", "SOMETHING ELSE LLC") };

        var result = TransferOrderVendorResolver.Resolve([], vendors, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Resolves_from_the_vendor_full_name()
    {
        var vendors = new[] { Vendor(111, "IGCI", "SOMETHING ELSE LLC", Scanned) };

        var result = TransferOrderVendorResolver.Resolve([], vendors, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Name_that_is_not_on_the_document_is_rejected_with_the_scan_and_the_candidates()
    {
        var result = TransferOrderVendorResolver.Resolve(Lines(), null, "SOME OTHER VENDOR, INC.", OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("SOME OTHER VENDOR, INC.", result.ErrorMessage);
        Assert.Contains(OrderNumber, result.ErrorMessage);
        Assert.Contains("Vendors on this order", result.ErrorMessage);
    }

    [Fact]
    public void Two_vendors_that_only_differ_by_punctuation_are_reported_as_ambiguous()
    {
        var lines = new List<TransferOrderLineDTO>
        {
            Line(1, 111, "ABC CO., LTD"),
            Line(2, 222, "ABC CO. LTD")
        };

        // Strict: no exact match. Loose: both collapse to "ABC CO LTD".
        var result = TransferOrderVendorResolver.Resolve(lines, null, "ABC CO, LTD", OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("more than one vendor", result.ErrorMessage);
    }

    [Fact]
    public void Blank_scan_asks_for_the_vendor_barcode()
    {
        var result = TransferOrderVendorResolver.Resolve(Lines(), null, "   ", OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("Scan the vendor barcode", result.ErrorMessage);
    }

    [Fact]
    public void Lines_without_a_vendor_are_ignored()
    {
        var lines = new List<TransferOrderLineDTO>
        {
            Line(1, null, null),
            Line(2, 111, Scanned)
        };

        var result = TransferOrderVendorResolver.Resolve(lines, null, Scanned, OrderNumber);

        Assert.True(result.Success);
        Assert.Equal(111, result.Data!.Id);
    }

    [Fact]
    public void Order_with_no_vendor_on_any_line_is_reported_clearly()
    {
        var lines = new List<TransferOrderLineDTO> { Line(1, null, null) };

        var result = TransferOrderVendorResolver.Resolve(lines, null, Scanned, OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("has no vendor on its lines", result.ErrorMessage);
    }

    [Fact]
    public void Vendor_ids_present_but_no_name_anywhere_is_reported_clearly()
    {
        var lines = new List<TransferOrderLineDTO> { Line(1, 111, null) };

        var result = TransferOrderVendorResolver.Resolve(lines, null, Scanned, OrderNumber);

        Assert.False(result.Success);
        Assert.Contains("could not be read", result.ErrorMessage);
    }
}
