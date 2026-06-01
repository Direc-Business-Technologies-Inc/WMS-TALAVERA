using Application.DataTransferObjects.Administration.Role;
using Application.DataTransferObjects.Commons;
using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Procurement.Order;

namespace Application.DataTransferObjects.Administration.User;

public class UserDTO : AuditableDTO
{
    public PersonNameDTO Name { get; set; }
    public EmailDTO Email { get; set; }
    public AccountDTO Account { get; set; }
    public RoleDTO Role { get; set; }

    public string? PhoneNumber { get; set; }
    public string Company { get; set; } = string.Empty;
    public bool Active { get; set; }

    public List<UserPermissionDTO> Permissions { get; set; } = [];
}
