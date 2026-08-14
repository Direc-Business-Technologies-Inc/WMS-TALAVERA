namespace Shared.Libraries.ViewModel.VendorReturnAuthorization;
public class VendorReturnAuthorizationLineVM : TransactionVM
{
    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public decimal PreferredBinQuantityAvailableGood { get; set; }
    public decimal PreferredBinQuantityAvailableBad { get; set; }

    public int NetsuiteMaterialVendorAssignedBin { get; set; }
    public decimal VendorAssignedBinQuantityAvailableGood { get; set; }
    public decimal VendorAssignedBinQuantityAvailableBad { get; set; }

    public decimal GoodPerUomRate =>
                UoMRate == 0
                    ? 0
                    : !IsLocationUsedBin ? LocationItemQuantityAvailableGood
                    : (NetsuiteMaterialVendorAssignedBin != 0
                        ? VendorAssignedBinQuantityAvailableGood
                        : PreferredBinQuantityAvailableGood) / UoMRate;

    public decimal BadPerUomRate =>
                UoMRate == 0
                    ? 0
                    : !IsLocationUsedBin ? LocationItemQuantityAvailableBad
                    : (NetsuiteMaterialVendorAssignedBin != 0
                        ? VendorAssignedBinQuantityAvailableBad
                        : PreferredBinQuantityAvailableBad) / UoMRate;
}
