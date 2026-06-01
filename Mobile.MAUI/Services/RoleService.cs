using static Mobile.MAUI.Enums.CustomEnum;

namespace Mobile.MAUI.Services;

public class RoleService
{
    PPCRole? _role;
    public RoleService()
    {

    }
    public async Task SetRole(PPCRole? role)
    {
        if (role is null)
        {
            SecureStorage.Remove("user-role");
            return;
        }
        _role = role;
        await SecureStorage.SetAsync("user-role", role.ToString());
    }
    public PPCRole? GetRole()
    {
        var roleString = SecureStorage.GetAsync("user-role").Result;
        if (int.TryParse(roleString, out int role) && Enum.IsDefined(typeof(PPCRole), role))
        {
            return (PPCRole)role;
        }

        return null;
    }
}
