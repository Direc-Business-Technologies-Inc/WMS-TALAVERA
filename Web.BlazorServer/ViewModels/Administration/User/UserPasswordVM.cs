namespace Web.BlazorServer.ViewModels.Administration.User;

public class UserPasswordVM
{
    public Guid UserId { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
