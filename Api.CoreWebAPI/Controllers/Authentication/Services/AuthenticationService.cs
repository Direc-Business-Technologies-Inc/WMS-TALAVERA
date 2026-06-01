using Api.CoreWebAPI.Controllers.Authentication.Repositories;
using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Repositories.Bases;
using DataCipher;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.CoreWebAPI.Controllers.Authentication.Services;

public class AuthenticationService(ISender Sender,
    IAppReadRepository appRead,
    IAppCommandRepository appCommand) : IAuthenticationService
{
    public string DecryptPassword(string encryptedPassword)
    {
        return Encryption.Decrypt(encryptedPassword);
    }

    public string EncryptPassword(string password)
    {
        return Encryption.Encrypt(password);
    }

    public async Task<string> GetJwtToken(UserDTO user)
    {
        try
        {
            List<Claim> permissionClaims = [];

            foreach (var permissions in user.Permissions)
            {
                permissionClaims.Add(new Claim("permission", permissions.Permission.Permission));
            }


            string? signingkey = Environment.GetEnvironmentVariable("WMSAPI_SIGN");
            if (signingkey is null) return null;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingkey));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Account.UserName.Value),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            claims.AddRange(permissionClaims);

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "wmsapi",
                audience: "com.wmsmobile.appname",
                claims: claims,
                expires: DateTime.Now.AddMinutes(480),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception)
        {

            throw;
        }
    }
}
