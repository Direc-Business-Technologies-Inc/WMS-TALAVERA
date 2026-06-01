namespace Application.DataTransferObjects.Administration.User;

public class UserPasswordDTO
{
    public Guid UserId { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
