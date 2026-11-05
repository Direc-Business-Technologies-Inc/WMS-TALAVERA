namespace Application.DataTransferObjects.Transactions.Packing.NS;

public static class VendorReturnAuthorizationInventoryAvailability
{
    public static bool CanFulfill(VendorReturnAuthorizationLineDTO line, bool isBad, decimal requestedQuantity) =>
        requestedQuantity <= 0 || requestedQuantity <= GetAvailableQuantity(line, isBad);

    public static decimal GetAvailableQuantity(VendorReturnAuthorizationLineDTO line, bool isBad)
    {
        var available = !IsNetSuiteTrue(line.LocationUsedBin)
            ? isBad ? line.LocationItemQuantityAvailableBad : line.LocationItemQuantityAvailableGood
            : line.NetsuiteMaterialVendorAssignedBin != 0
                ? isBad ? line.VendorAssignedBinQuantityAvailableBad : line.VendorAssignedBinQuantityAvailableGood
                : isBad ? line.PreferredBinQuantityAvailableBad : line.PreferredBinQuantityAvailableGood;

        return line.UoMRate == 0 ? available : available / line.UoMRate;
    }

    public static int? GetSelectedBinId(VendorReturnAuthorizationLineDTO line)
    {
        if (!IsNetSuiteTrue(line.LocationUsedBin)) return null;

        return line.NetsuiteMaterialVendorAssignedBin != 0
            ? line.NetsuiteMaterialVendorAssignedBin
            : line.NetsuiteMaterialPrefferedBinId != 0
                ? line.NetsuiteMaterialPrefferedBinId
                : null;
    }

    private static bool IsNetSuiteTrue(string value) =>
        value.Equals("T", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("true", StringComparison.OrdinalIgnoreCase);
}
