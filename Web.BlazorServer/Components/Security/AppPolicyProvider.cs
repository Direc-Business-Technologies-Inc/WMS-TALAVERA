using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Web.BlazorServer.Components.Security;

public class AppPolicyProvider : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public AppPolicyProvider(IOptions<AuthorizationOptions> options) => FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        List<IAuthorizationRequirement> requirements = [];

        if (policyName.StartsWith("permission", StringComparison.OrdinalIgnoreCase))
            requirements.Add(new PermissionRequirement(policyName));

        if(requirements.Count > 0)
        {
            var policy = new AuthorizationPolicyBuilder();

            foreach (var req in requirements)
                policy.AddRequirements(req);

            return Task.FromResult<AuthorizationPolicy?>(policy.Build());
        }

        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
