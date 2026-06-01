using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.Markers;

namespace Domain.Entities.Administration.User.Role;

public class RoleDEM : AuditableDEM, IAggregateRoot, IHasCode, IHasName
{
    public bool Active { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }

    readonly List<RolePermissionDEM> _permissions = [];
    public IReadOnlyCollection<RolePermissionDEM> Permissions => _permissions.AsReadOnly();

    public RoleDEM() { }

    RoleDEM(string code, string name, IEnumerable<RolePermissionDEM>? permissions = null)
    {
        Code = Guard.Against.NullOrEmpty(code, nameof(code), "Role code cannot be null or empty");
        Name = Guard.Against.NullOrEmpty(name, nameof(name), "Role name cannot be null or empty");
        Active = true;
        if (permissions is not null)
            foreach (var permission in permissions)
                this.AddPermission(permission);
    }

    public static RoleDEM Create(string code, string name, IEnumerable<RolePermissionDEM>? permissions = null)
    {
        return new(code, name, permissions);
    }

    public RoleDEM SetActiveStatus(bool status)
    {
        Active = Guard.Against.Null(status, nameof(status), "Role active status cannot be null");
        return this;
    }

    public RoleDEM ChangeCode(string code)
    {
        Code = Guard.Against.NullOrEmpty(code, nameof(code), "Role code cannot be null or empty");
        return this;
    }

    public RoleDEM ChangeName(string name)
    {
        Name = Guard.Against.NullOrEmpty(name, nameof(name), "Role name cannot be null or empty");
        return this;
    }

    public RoleDEM AddPermission(RolePermissionDEM permission)
    {
        _permissions.Add(permission);

        return this;
    }

    public RoleDEM AddPermission(IEnumerable<RolePermissionDEM> permissions)
    {
        _permissions.AddRange(permissions);

        return this;
    }

    public RoleDEM RemovePermission(RolePermissionDEM permission)
    {
        _permissions.Remove(permission);

        return this;
    }

    public RoleDEM ClearPermissions()
    {
        _permissions.Clear();

        return this;
    }

}
