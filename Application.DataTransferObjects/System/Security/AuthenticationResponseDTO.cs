using Application.DataTransferObjects.Administration.User;
using System.Runtime.CompilerServices;

namespace Application.DataTransferObjects.System.Security;

public class AuthenticationResponseDTO
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public UserDTO? User { get; set; } = null;

    public static AuthenticationResponseDTO Success(string message, UserDTO user)
    {
        AuthenticationResponseDTO response = new()
        {
            IsSuccess = true,
            Message = message,
            User = user
        };

        return response;
    }

    public static AuthenticationResponseDTO Fail(string message)
    {
        AuthenticationResponseDTO response = new()
        {
            IsSuccess = false,
            Message = message,
            User = null
        };

        return response;
    }
}
