using Domain.Providers;

namespace Web.BlazorServer.ViewModels.Transaction.GoodsIssue;

public class GoodsIssueDataGridVM
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate { get; set; } = DateTimeProvider.Now;
    public string PreparedBy { get; set; } = string.Empty;
    public string TransTypeCode { get; set; } = string.Empty;
    public string TransTypeName { get; set; } = string.Empty;
    public string AcctCode { get; set; } = string.Empty;
    public string AcctName { get; set; } = string.Empty;
}
