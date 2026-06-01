using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mobile.MAUI.Services;

public class JWTAuthStateProvider : AuthenticationStateProvider
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()

    {

        try
        {

            var token = await SecureStorage.GetAsync("access-token");

            if (string.IsNullOrWhiteSpace(token) || IsTokenExpired(token))
            {
                NotifyUserLogout();
                return new AuthenticationState(_anonymous);
            }


            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (Exception e)
        {

            return new AuthenticationState(_anonymous);
        }
    }


    public async Task NotifyUserAuthentication(string accessToken)
    {
        await SecureStorage.SetAsync("access-token", accessToken);

        var claims = ParseClaimsFromJwt(accessToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        SecureStorage.Remove("access-token");

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private bool IsTokenExpired(string token)
    {
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var timeRemaining = jwtToken.ValidTo - DateTime.UtcNow;
        return timeRemaining.TotalMinutes <= 1;
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var jwtToken = _tokenHandler.ReadJwtToken(jwt);
        return jwtToken.Claims;
    }
    public string? GetAccessToken()
    {
        return SecureStorage.GetAsync("access-token").Result;
    }
}
