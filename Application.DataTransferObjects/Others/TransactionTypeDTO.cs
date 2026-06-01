namespace Application.DataTransferObjects.Others;

public class TransactionTypeDTO
{
    public string Code { get; set; }
    public string Name { get; set; }
    public GLAccountDTO Account { get; set; }
}
