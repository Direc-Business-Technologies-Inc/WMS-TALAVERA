using Application.DataTransferObjects.Administration.User;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Commons;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Administration.User;

public class UserVM : AuditableVM
{
    public PersonNameVM Name { get; set; } = new();
    public EmailVM Email { get; set; } = new();
    public AccountVM Account { get; set; } = new();
    public RoleVM? Role { get; set; } = null;

    public string? PhoneNumber { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string BiometricsId { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    public IEnumerable<LoginDTO> LoginHistory { get; set; } = [];
}
