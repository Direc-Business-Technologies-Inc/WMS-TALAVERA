using Microsoft.AspNetCore.Authorization;

namespace Web.BlazorServer.Components.Security;

public class AppAuthorizationAttribute : AuthorizeAttribute
{
    public AppAuthorizationAttribute(string action, string resource) => Policy = $"permission.{resource.ToUpper()}.{action.ToUpper()}";
}
