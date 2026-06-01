using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.Events.Bases;
using Domain.Markers;
using Domain.ValueObjects.Others;

namespace Domain.Entities.Administration.User.Management;

public class UserDEM : AuditableDEM, IAggregateRoot, IActivateable
{
    public PersonNameVO Name { get; private set; }
    public EmailVO Email { get; private set; }
    public AccountVO Account { get; private set; }

    public string? PhoneNumber { get; private set; }
    public string Company { get; private set; }
    public bool Active { get; private set; }
    public Guid RoleId { get; private set; }


    readonly List<LoginDEM> _loginHistory = [];
    public IReadOnlyCollection<LoginDEM> LoginHistory => _loginHistory.AsReadOnly();

    readonly List<UserPermissionDEM> _permissions = [];
    public IReadOnlyCollection<UserPermissionDEM> Permissions => _permissions.AsReadOnly();

    UserDEM() { }

    UserDEM(PersonNameVO name, EmailVO email, AccountVO account, string company, Guid roleId, string? phoneNumber = null, IEnumerable<UserPermissionDEM>? permissions = null)
    {
        Name = Guard.Against.Null(name, nameof(name), "User name cannot be null");
        Email = Guard.Against.Null(email, nameof(email), "User email cannot be null");
        Company = Guard.Against.NullOrEmpty(company, nameof(company), "User company cannot be null or empty");
        RoleId = Guard.Against.Null(roleId, nameof(roleId), "User role ID cannot be null or empty");
        Account = Guard.Against.Null(account, nameof(account), "User cannot be null");
        Active = true;
        PhoneNumber = phoneNumber;
        if (permissions is not null)
            foreach (var permission in permissions)
                this.AddPermission(permission);
        DomainEvents.Add(new EntityCreatedEvent<UserDEM>(this));
    }

    public static UserDEM Create(PersonNameVO name, EmailVO email, AccountVO account, string company, Guid roleId, string? phoneNumber = null, IEnumerable<UserPermissionDEM>? permissions = null)
    {
        return new(
            name,
            email,
            account,
            company,
            roleId,
            phoneNumber,
            permissions
            )
        {

        };
    }

    public UserDEM Update(PersonNameVO name, EmailVO email, AccountVO account, string company, Guid roleId, string? phoneNumber = null)
    {
        Name = Guard.Against.Null(name, nameof(name), "User name cannot be null");
        Email = Guard.Against.Null(email, nameof(email), "User email cannot be null");
        Company = Guard.Against.NullOrEmpty(company, nameof(company), "User company cannot be null or empty");
        RoleId = Guard.Against.Null(roleId, nameof(roleId), "User role ID cannot be null or empty");
        Account = Guard.Against.Null(account, nameof(account), "User cannot be null");
        Active = true;
        PhoneNumber = phoneNumber;

        return this;
    }

    public UserDEM ChangePassword(string hashedPassword)
    {
        Account.ChangePassword(Guard.Against.Null(hashedPassword, nameof(hashedPassword), "Hashed Password cannot be null"));
        return this;
    }

    public UserDEM ChangeName(PersonNameVO name)
    {
        Name = Guard.Against.Null(name, nameof(name), "User name cannot be null");
        return this;
    }

    public UserDEM ChangeEmail(EmailVO email)
    {
        Email = Guard.Against.Null(email, nameof(email), "User email cannot be null");
        return this;
    }

    public UserDEM ChangePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        return this;
    }

    public UserDEM ChangeCompany(string company)
    {
        Company = Guard.Against.NullOrEmpty(company, nameof(company), "User company cannot be null or empty");
        return this;
    }

    public UserDEM Activate()
    {
        Active = true;
        return this;
    }

    public UserDEM Deactivate()
    {
        Active = false;
        return this;
    }

    public UserDEM ChangeRole(Guid roleId)
    {
        RoleId = Guard.Against.Null(roleId, nameof(roleId), "User role ID cannot be null or empty");
        return this;
    }

    public UserDEM AddNewLogin(LoginDEM login)
    {
        _loginHistory.Add(login);
        return this;
    }

    public UserDEM AddPermission(UserPermissionDEM permission)
    {
        _permissions.Add(permission);

        return this;
    }

    public UserDEM AddPermission(IEnumerable<UserPermissionDEM> permissions)
    {
        _permissions.AddRange(permissions);

        return this;
    }

    public UserDEM RemovePermission(UserPermissionDEM permission)
    {
        _permissions.Remove(permission);

        return this;
    }

    public UserDEM ClearPermissions()
    {
        _permissions.Clear();

        return this;
    }
}
