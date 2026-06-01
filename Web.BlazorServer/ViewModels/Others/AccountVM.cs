namespace Web.BlazorServer.ViewModels.Others;

public class AccountVM
{
    public UserNameVM UserName { get; set; } = new();
    public string HashedPassword { get; set; } = string.Empty;
    public bool Locked { get; set; } = false;
    public bool LockoutEnabled { get; set; } = true;
}
