namespace Web.BlazorServer.ViewModels.Others;

public class TransactionTypeVM
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GLAccountVM Account { get; set; } = new();
}
