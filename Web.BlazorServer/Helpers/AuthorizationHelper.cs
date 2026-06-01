using Web.BlazorServer.Components.Security;

namespace Web.BlazorServer.Helpers;

public class AuthorizationHelper(AppAuthenticationService appAuth)
{
    public bool View(string module) => appAuth.HasPermission($"{module}.VIEW");
    public bool Create(string module) => appAuth.HasPermission($"{module}.CREATE");
    public bool Update(string module) => appAuth.HasPermission($"{module}.UPDATE");
    public bool Archive(string module) => appAuth.HasPermission($"{module}.ARCHIVE");
    public bool Do(string module, string permission) => appAuth.HasPermission($"{module}.{permission}");
}
