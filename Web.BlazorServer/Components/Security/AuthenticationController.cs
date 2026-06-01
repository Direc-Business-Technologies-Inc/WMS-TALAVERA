using Application.DataTransferObjects.System.Modules;
using Application.DataTransferObjects.System.Security;
using Application.UseCases.Commands.System.Authentication;
using Application.UseCases.Queries.System.Authentication;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Web.BlazorServer.ViewModels.Security;

namespace Web.BlazorServer.Components.Security;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(
    AppAuthenticationStateProvider AppAuthenticationState,
    IHttpContextAccessor HttpContextAccessor,
    ISender Sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var authState = await AppAuthenticationState.GetAuthenticationStateAsync();

        if (authState.User.Identity?.IsAuthenticated is true)
        {
            return Redirect("/dashboard");
        }
        else
        {
            return Redirect("/");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] AuthenticationVM login)
    {
        List<string> permissions = [];
        AuthenticationPayloadDTO dto = login.Adapt<AuthenticationPayloadDTO>();

        LoginCmd cmd = new(dto);
        var loginResponse = await Sender.Send(cmd);

        if (!loginResponse.IsSuccess)
            return Unauthorized(new { message = loginResponse.Message });

        if (loginResponse.User is not null)
        {
            GetUserModulePermissionsQry qry = new(loginResponse.User.Id);
            IEnumerable<ModulePermissionDTO> permissionResponse = await Sender.Send(qry);
            permissions = [.. permissionResponse.Select(x => $"{x.ModuleCode.ToUpper()}.{x.Permission.ToUpper()}")];
        }

        string permissionString = JsonSerializer.Serialize(permissions);

        List<Claim> claims =
        [
            new Claim("Id", loginResponse.User is null ? Guid.Empty.ToString() : loginResponse.User.Id.ToString()),
            new Claim("Name", loginResponse.User is null ? "LSMS User" : loginResponse.User.Name.FullName),
            new Claim("RoleId", loginResponse.User is null ? Guid.Empty.ToString() : loginResponse.User.Role.Id.ToString()),
            new Claim("Role", loginResponse.User is null ? Guid.Empty.ToString() : loginResponse.User.Role.Name),
            new Claim("Email", loginResponse.User is null ? "user@example.com" : loginResponse.User.Email.Address),
            new Claim("Permissions", permissionString)
        ];

        await HttpContextAccessor!.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        AppAuthenticationState.NotifyAuthenticationStateChanged();

        return Ok();
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContextAccessor!.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok();
    }
}
