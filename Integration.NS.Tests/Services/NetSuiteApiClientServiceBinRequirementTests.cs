using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Whether a posted packing line must carry a bin.
/// </summary>
/// <remarks>
/// Both bin ids reach the WMS through LEFT JOINs in the packing scripts
/// (NS_TO_x_Packing_Get_Items.sql and siblings), so a location whose
/// location.usebins = 'F' resolves them to 0 by construction. The pre-flight guard
/// tested only "scanned and both ids are 0", never consulting the bin-tracked
/// flag, so it rejected legitimate saves at non-bin locations and told the
/// operator the location was bin-tracked when it was not. Field failure
/// 2026-10-15: SELLING AREA - CAUAYAN (usebins = 'F'), item 00018051, 150 Good,
/// surfaced as "Cannot post GOOD TO Item Fulfillment: ... is bin-tracked but no
/// bin is resolved for ...".
/// </remarks>
public class NetSuiteApiClientServiceBinRequirementTests
{
    private static bool Requires(bool isUsedBin, decimal scannedQuantity = 150, int vendorAssignedBin = 0, int preferredBin = 0) =>
        NetSuiteApiClientService.RequiresBin(isUsedBin, scannedQuantity, vendorAssignedBin, preferredBin);

    // Production repro: SELLING AREA - CAUAYAN and SELLING AREA - CAUAYAN JR are
    // both usebins = 'F'. No bin is expected and none may be demanded.
    [Theory]
    [InlineData(false)]
    public void Non_bin_tracked_location_does_not_require_a_bin(bool isUsedBin)
    {
        Assert.False(Requires(isUsedBin));
    }

    // The real validation still fires: bin-tracked location, no bin resolved.
    [Fact]
    public void Bin_tracked_location_without_a_bin_is_blocked() =>
        Assert.True(Requires(isUsedBin: true));

    [Theory]
    [InlineData(0, 777)]   // preferred bin satisfies the requirement
    [InlineData(444, 0)]   // vendor-assigned bin satisfies it and outranks preferred
    [InlineData(444, 777)]
    public void Any_resolved_bin_satisfies_the_requirement(int vendorAssignedBin, int preferredBin) =>
        Assert.False(Requires(isUsedBin: true, vendorAssignedBin: vendorAssignedBin, preferredBin: preferredBin));

    // Zero-qty filler lines are posted with an empty inventoryAssignment and never
    // carry a bin; they must not be blocked.
    [Fact]
    public void Zero_quantity_line_never_requires_a_bin() =>
        Assert.False(Requires(isUsedBin: true, scannedQuantity: 0));

    // Location flag is the only gate that matters - both bin ids at zero is the
    // normal state of a non-bin location, not missing data.
    [Fact]
    public void Gate_depends_on_location_flag_not_on_bin_ids() =>
        Assert.NotEqual(
            Requires(isUsedBin: false),
            Requires(isUsedBin: true));
}