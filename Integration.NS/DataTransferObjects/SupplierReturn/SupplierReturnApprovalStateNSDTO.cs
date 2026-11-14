namespace Integration.NS.DataTransferObjects.SupplierReturn;

public sealed class SupplierReturnApprovalStateNSDTO
{
    public int Id { get; set; }
    public int? SubsidiaryId { get; set; }
    public string SubmittedForApprovals { get; set; } = string.Empty;
    public int? ApprovalStatusId { get; set; }
}
