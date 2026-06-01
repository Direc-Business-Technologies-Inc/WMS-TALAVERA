using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Helpers.Extensions;

public static class AuthenticationStateExtension
{
    public static async Task<string> GetAuthenticatedUserId(this Task<AuthenticationState> authStateTask)
    {
        var authState = await authStateTask;
        var user = authState.User;
        if (user != null && user.Identity is not null && user.Identity.IsAuthenticated)
        {
            return user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
        return string.Empty;
    }

    public static async Task<bool> Authenticated(this Task<AuthenticationState> authStateTask)
    {
        var authState = await authStateTask;
        var user = authState.User;
        return user != null && user.Identity is not null && user.Identity.IsAuthenticated;
    }
}
