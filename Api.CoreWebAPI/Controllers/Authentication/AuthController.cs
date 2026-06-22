using Api.CoreWebAPI.Controllers.Authentication.Repositories;
using Application.DataTransferObjects.System.Security;
using Application.UseCases.Commands.System.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;

namespace Api.CoreWebAPI.Controllers.Authentication
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ISender Sender, IAuthenticationService Auth) : ControllerBase
    {
        [HttpPost("Login")]
        public async Task<ApiResult<string>> Login([FromBody] AuthenticationPayloadDTO reqbody)
        {
            LoginCmd cmd = new(reqbody);
            var loginResponse = await Sender.Send(cmd);

            if (!loginResponse.IsSuccess)
            {
                return ApiResult<string>.Failed(loginResponse.Message);
            }

            var token = await Auth.GetJwtToken(loginResponse.User);

            return ApiResult<string>.Succeeded(token);
        }
    }
}
