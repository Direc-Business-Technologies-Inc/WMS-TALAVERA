using Microsoft.AspNetCore.Authorization;

namespace Web.BlazorServer.Components.Security;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; private set; } = permission;
}
