using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.ViewModels.Security;

public class AuthenticationVM
{
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }
}
