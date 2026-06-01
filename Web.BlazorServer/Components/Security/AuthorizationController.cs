using Microsoft.AspNetCore.Authorization;

namespace Web.BlazorServer.Components.Security;

public class AuthorizationController(AppAuthenticationService appAuthenticationService) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if(appAuthenticationService.IsAuthenticated())
        {
            string requirementString = requirement.Permission;
            string[] requirements = requirementString.Split('.');

            if(!string.IsNullOrEmpty(requirements[1]) && !string.IsNullOrEmpty(requirements[2]))
            {
                bool response = appAuthenticationService.HasPermission($"{requirements[1]}.{requirements[2]}");

                if(response)
                    context.Succeed(requirement);
            }
        }
    }
}
