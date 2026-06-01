namespace Application.DataTransferObjects.Others;

public class AccountDTO
{
    public UserNameDTO UserName { get; set; }
    public string HashedPassword { get; set; }
    public bool Locked { get; set; }
    public bool LockoutEnabled { get; set; }
}
