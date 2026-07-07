using Microsoft.AspNetCore.Components.Authorization;
using Shared.Services.Repository;
using System.Security.Claims;

namespace Web.BlazorServer.Components.Security;

public class AppAuthenticationStateProvider : AuthenticationStateProvider
{
    AuthenticationState authenticationState { get; set; }

    public AppAuthenticationStateProvider(AppAuthenticationService service, ICurrentUserService currentUserService)
    {
        authenticationState = new AuthenticationState(service.CurrentUser);
        SyncCurrentUser(service.CurrentUser, currentUserService);

        service.UserChanged += (newUser) =>
        {
            authenticationState = new AuthenticationState(newUser);
            SyncCurrentUser(newUser, currentUserService);

            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(newUser)));
        };
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(authenticationState);

    public void NotifyAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    static void SyncCurrentUser(ClaimsPrincipal user, ICurrentUserService currentUserService)
    {
        if (user.Identity?.IsAuthenticated != true) return;

        var idClaim = user.FindFirst("Id")?.Value;
        var nameClaim = user.FindFirst("Name")?.Value ?? string.Empty;
        var nsSubsidiaryClaim = user.FindFirst("NsSubsidiaryId")?.Value;
        int.TryParse(nsSubsidiaryClaim, out var nsSubsidiaryId);

        if (Guid.TryParse(idClaim, out var userId))
            currentUserService.SetUser(userId, nameClaim, nsSubsidiaryId);
    }
}
