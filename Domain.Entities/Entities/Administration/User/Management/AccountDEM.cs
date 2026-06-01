using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.ValueObjects.Others;

namespace Domain.Entities.Administration.User.Management;

public class AccountDEM : AuditableDEM
{
    public UserNameVO UserName { get; private set; }
    public string HashedPassword { get; private set; }
    public bool Locked { get; private set; }
    public bool LockoutEnabled { get; private set; }

    AccountDEM() { }

    public AccountDEM(UserNameVO userName, string hashedPassword, bool lockoutStatus = true)
    {
        UserName = Guard.Against.Null(userName, nameof(userName), "User's username cannot be null");
        HashedPassword = Guard.Against.Null(hashedPassword, nameof(hashedPassword), "User's password cannot be null");
        LockoutEnabled = Guard.Against.Null(lockoutStatus, nameof(lockoutStatus), "Lockout status cannot be null");
        Locked = false;
    }

    public static AccountDEM Create(UserNameVO userName, string hashedPassword)
    {
        return new(
            userName,
            hashedPassword
            );
    }

    public AccountDEM ChangeUserName(UserNameVO userName)
    {
        UserName = Guard.Against.Null(userName, nameof(userName), "User's username cannot be null");
        return this;
    }

    public AccountDEM ChangePassword(string hashedPassword)
    {
        HashedPassword = Guard.Against.Null(hashedPassword, nameof(hashedPassword), "User's password cannot be null");
        return this;
    }

    public AccountDEM ChangeLockoutStatus(bool lockoutStatus)
    {
        LockoutEnabled = Guard.Against.Null(lockoutStatus, nameof(lockoutStatus), "Lockout status cannot be null");
        return this;
    }

    public AccountDEM LockAccount()
    {
        Locked = true;
        return this;
    }

    public AccountDEM UnlockAccount()
    {
        Locked = false;
        return this;
    }
}
