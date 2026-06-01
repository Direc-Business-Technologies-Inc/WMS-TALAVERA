using Application.DataTransferObjects.Administration.User;

namespace Api.CoreWebAPI.Controllers.Authentication.Repositories
{
    public interface IAuthenticationService
    {
        string DecryptPassword(string encryptedPassword);
        string EncryptPassword(string password);
        Task<string> GetJwtToken(UserDTO user);
    }
}
