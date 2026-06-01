using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Commands.System.Authentication;
using Application.UseCases.Queries.Administration.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;
using Sprache;

namespace Api.CoreWebAPI.Controllers.User
{
    [ApiController]
    //[Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/[controller]")]
    public class UserController(ISender Sender) : ControllerBase
    {
        [HttpPost("get-profile")]
        public async Task<ApiResult<SignedInUserVM>> GetUserProfile(UserProfileRequestDTO req)
        {
            Guid.TryParse(req.UserId, out Guid newId);

            GetUserQry cmd = new(newId);
            var res = await Sender.Send(cmd);

            SignedInUserVM userVM = new()
            {
                FullName = res.Name.FullName,
                UserName = res.Account.UserName.Value,
                PositionName = res.Role.Name
            };

            return ApiResult<SignedInUserVM>.Succeeded(userVM);
        }
    }
}
