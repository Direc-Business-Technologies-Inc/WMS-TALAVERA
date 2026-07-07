using Api.CoreWebAPI.Controllers.Authentication.Repositories;
using Application.DataTransferObjects.System.Security;
using Application.UseCases.Commands.System.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel.Authentication;

namespace Api.CoreWebAPI.Controllers.Authentication
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ISender Sender, IAuthenticationService Auth) : ControllerBase
    {
        [HttpPost("Login")]
        public async Task<ApiResult<AuthenticationVM>> Login([FromBody] AuthenticationPayloadDTO reqbody)
        {
            LoginCmd cmd = new(reqbody);
            var loginResponse = await Sender.Send(cmd);

            if (!loginResponse.IsSuccess)
            {
                return ApiResult<AuthenticationVM>.Failed(loginResponse.Message);
            }

            var token = await Auth.GetJwtToken(loginResponse.User);

            AuthenticationVM authVM = new()
            {
                Token = token,
                NetsuiteSubsidiaryInternalId = loginResponse.User.EmployeeNs.NsSubsidiaryId,
                NetsuiteDepartmentInternalId = loginResponse.User.EmployeeNs.NsDepartmentId,
                NetsuiteEmployeeInternalId = loginResponse.User.EmployeeNs.NsId
            };

            return ApiResult<AuthenticationVM>.Succeeded(authVM);
        }
    }
}
