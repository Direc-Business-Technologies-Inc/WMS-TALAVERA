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
}

