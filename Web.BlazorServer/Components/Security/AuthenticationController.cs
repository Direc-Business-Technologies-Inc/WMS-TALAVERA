using Application.DataTransferObjects.System.Modules;
using Application.DataTransferObjects.System.Security;
using Application.UseCases.Commands.System.Authentication;
using Application.UseCases.Queries.System.Authentication;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities;
using System.Security.Claims;
using System.Text.Json;
using Web.BlazorServer.ViewModels.Security;

namespace Web.BlazorServer.Components.Security;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController(
    AppAuthenticationStateProvider AppAuthenticationState,
    IHttpContextAccessor HttpContextAccessor,
    INetsuiteIdentityIntegration nsIdentityIntegration,
    ILocationIntegration locationIntegration,
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
            new Claim("NsSubsidiaryId", loginResponse.User?.EmployeeNs?.NsSubsidiaryId.ToString() ?? "0"),
            new Claim("Permissions", permissionString)
        ];

        if (loginResponse.User?.EmployeeNs is not null) //claims for ns
        {
            var nsIdentity = await nsIdentityIntegration.GetNetsuiteIdentityAsync(loginResponse.User.EmployeeNs.NsId);
            var userLocations = await locationIntegration.GetUserAllowedLocations(new DataGridIntent { Take = -1 }, loginResponse.User.EmployeeNs.NsId);

            int[] userLocationIds = userLocations.data.Any() ? [..userLocations.data.Select(x => x.Id)] : [-1];
            claims.Add(new Claim("com.direcbusiness.wms.nsEmployeeId", loginResponse.User.EmployeeNs.NsId.ToString()));

            if (nsIdentity is not null)
            {
                claims.Add(new Claim("com.direcbusiness.wms.nsEmployeeName", nsIdentity.EmployeeFullName));
                claims.Add(new Claim("com.direcbusiness.wms.nsSubsidiary", nsIdentity.SubsidiaryID.ToString()));
                claims.Add(new Claim("com.direcbusiness.wms.nsAllowedLocations", JsonSerializer.Serialize(userLocationIds)));
                claims.Add(new Claim("com.direcbusiness.wms.nsAllowedSubsidiaries", JsonSerializer.Serialize(new int[] { nsIdentity.SubsidiaryID })));
            }
        }

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
