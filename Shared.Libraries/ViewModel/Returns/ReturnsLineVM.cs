namespace Shared.Libraries.ViewModel.Returns;

public class ReturnsLineVM : TransactionVM
{
    public int TransferCategory { get; set; }

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

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

