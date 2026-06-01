namespace Application.DataTransferObjects.Administration.User;

public class UserDetailDTO
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}
