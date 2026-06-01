using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.ValueObjects.Others;

namespace Domain.Entities.System;

public class ModuleDEM : EntityDEM
{
    public string Name { get; private set; }
    public bool Active { get; private set; }
    public string Code { get; private set; }
    public bool Root { get; private set; }
    public bool Transactional { get; private set; }
    public Guid? NavRouteId { get; private set; }

    readonly List<PermissionVO> _modulePermissions = [];
    public IReadOnlyCollection<PermissionVO> ModulePermissions => _modulePermissions.AsReadOnly();

    public ModuleDEM() { }

    public ModuleDEM(string name, string code, bool transactional = false, bool root = false, Guid? navRouteId = null, IEnumerable<PermissionVO>? permissions = null)
    {
        Name = Guard.Against.NullOrEmpty(name, nameof(name), "Name cannot be empty or null");
        Root = Guard.Against.Null(root, nameof(root), "Root Status cannot be null");
        Code = Guard.Against.NullOrEmpty(code, nameof(code), "Code cannot be empty or null");
        Transactional = Guard.Against.Null(transactional, nameof(transactional), "Transactional Status cannot be null");
        NavRouteId = navRouteId;
        Active = true;
        if (permissions is not null)
            foreach (var permission in permissions)
                this.AddPermission(permission);
    }
    public static ModuleDEM Create(string name, string code, bool transactional = false, bool root = false, Guid? navRouteId = null, IEnumerable<PermissionVO>? permissions = null)
    {
        ModuleDEM module = new(name, code, transactional, root, navRouteId, permissions);

        return module;
    }

    public ModuleDEM Update(string name, string code, bool active, bool root, bool transactional, Guid? navRouteId = null)
    {
        Name = Guard.Against.NullOrEmpty(name, nameof(name), "Name cannot be empty or null");
        Active = Guard.Against.Null(active, nameof(active), "Active Status cannot be null");
        Root = Guard.Against.Null(root, nameof(root), "Root Status cannot be null");
        Code = Guard.Against.NullOrEmpty(code, nameof(code), "Code cannot be empty or null");
        Transactional = Guard.Against.Null(transactional, nameof(transactional), "Transactional Status cannot be null");
        NavRouteId = navRouteId;

        return this;
    }

    public ModuleDEM AddPermission(PermissionVO permission)
    {
        _modulePermissions.Add(permission);

        return this;
    }

    public ModuleDEM RemovePermission(PermissionVO permission)
    {
        _modulePermissions.Remove(permission);

        return this;
    }

    public void SetAsTransactional() => Transactional = true;
    public void UnsetAsTransactional() => Transactional = false;
    public void Activate() => Active = true;
    public void Deactivate() => Active = false;

}
