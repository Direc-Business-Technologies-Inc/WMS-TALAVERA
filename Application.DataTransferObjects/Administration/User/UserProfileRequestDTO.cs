namespace Application.DataTransferObjects.Administration.User;

public record UserProfileRequestDTO
{
    public required string UserId { get; set; } = string.Empty;
}
