using Shared.Libraries.ViewModel.TransferOrder;
using Xunit;

namespace Integration.NS.Tests.ViewModels;

/// <summary>
/// Pins <see cref="TransferOrderLineVM.PackableQuantity"/> - the single
/// display-unit figure the device's white box, green state and scan guards all
/// derive from.
///
/// Background (2026-10-23 over-scan report, STR-THM00100000867, item 00032678):
/// transactionline.quantity is raw stock units (888 eaches) while
/// ScannedQuantity accumulates in the line's display unit (boxes, rate 12), and
/// the old guard compared the two directly - a ceiling of 888 scans on a line
/// planned for 74 boxes.
/// </summary>
public class TransferOrderLineVMPackableQuantityTests
{
    private static TransferOrderLineVM Line(
        decimal lineQuantity,
        decimal uomRate,
        decimal lineQuantityPacked = 0,
        decimal quantityAlreadyShipped = 0,
        decimal lineQuantityBackOrdered = 0,
        decimal scannedQuantity = 0)
        => new()
        {
            LineQuantity = lineQuantity,
            UoMRate = uomRate,
            LineQuantityPacked = lineQuantityPacked,
            QuantityAlreadyShipped = quantityAlreadyShipped,
            LineQuantityBackOrdered = lineQuantityBackOrdered,
            ScannedQuantity = scannedQuantity
        };

    [Fact]
    public void Line_quantity_is_converted_to_display_units()
    {
        // 888 eaches at BOX12 -> 74 boxes packable, exactly the query figure.
        var line = Line(lineQuantity: 888, uomRate: 12);

        Assert.Equal(74m, line.PackableQuantity);
    }

    [Fact]
    public void Fully_packed_line_has_nothing_left_to_pack()
    {
        var line = Line(lineQuantity: 276, uomRate: 12, lineQuantityPacked: 276);

        Assert.Equal(0m, line.PackableQuantity);
    }

    [Fact]
    public void Fully_shipped_line_has_nothing_left_even_when_packed_is_stale()
    {
        // quantitypacked can stay 0 on a row the packer never touched; the
        // shipped figure must still close the line.
        var line = Line(lineQuantity: 276, uomRate: 12, quantityAlreadyShipped: 276);

        Assert.Equal(0m, line.PackableQuantity);
    }

    [Fact]
    public void Partially_packed_line_keeps_only_its_open_boxes()
    {
        var line = Line(lineQuantity: 888, uomRate: 12, lineQuantityPacked: 444);

        Assert.Equal(37m, line.PackableQuantity);
    }

    [Fact]
    public void Fully_backordered_line_packs_zero()
    {
        // SuiteQL on the SHIPPING line: quantity 888, backordered 888. The white
        // box always showed 0 for this line; the guard now agrees.
        var line = Line(lineQuantity: 888, uomRate: 12, lineQuantityBackOrdered: 888);

        Assert.Equal(0m, line.PackableQuantity);
        Assert.True(line.PackedToPlan);
    }

    [Fact]
    public void Missing_uom_rate_falls_back_to_one_to_one()
    {
        var line = Line(lineQuantity: 50, uomRate: 0, quantityAlreadyShipped: 10);

        Assert.Equal(40m, line.PackableQuantity);
    }

    [Fact]
    public void Green_state_turns_only_at_the_packable_quantity()
    {
        var line = Line(lineQuantity: 276, uomRate: 12);

        line.ScannedQuantity = 22m;
        Assert.False(line.PackedToPlan);

        line.ScannedQuantity = 23m;
        Assert.True(line.PackedToPlan);
    }

    [Fact]
    public void Zero_plan_line_never_accepts_a_scan()
    {
        var line = Line(lineQuantity: 276, uomRate: 12, quantityAlreadyShipped: 276, scannedQuantity: 1);

        Assert.Equal(0m, line.PackableQuantity);
        Assert.True(line.PackedToPlan);
    }
}
