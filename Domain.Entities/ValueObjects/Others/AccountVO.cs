using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class AccountVO : ValueObject
{
    public UserNameVO UserName { get; private set; }
    public string HashedPassword { get; private set; }
    public bool Locked { get; private set; }
    public bool LockoutEnabled { get; private set; }

    public AccountVO() { }

    public AccountVO(UserNameVO username, string hashedPassword, bool lockoutEnabled = true, bool locked = false)
    {
        UserName = Guard.Against.Null(username, nameof(username), "Username cannot be null");
        HashedPassword = Guard.Against.NullOrEmpty(hashedPassword, nameof(hashedPassword), "Password cannot be null or empty"); ;
        Locked = Guard.Against.Null(locked, nameof(locked), "Lock Status cannot be null");
        LockoutEnabled = Guard.Against.Null(lockoutEnabled, nameof(lockoutEnabled), "Lockout Eligibility cannot be null");
    }

    public void SetValue(UserNameVO username, string hashedPassword, bool lockoutEnabled = true, bool locked = false)
    {
        UserName = Guard.Against.Null(username, nameof(username), "Username cannot be null");
        HashedPassword = Guard.Against.NullOrEmpty(hashedPassword, nameof(hashedPassword), "Password cannot be null or empty");
        Locked = Guard.Against.Null(locked, nameof(locked), "Lock Status cannot be null");
        LockoutEnabled = Guard.Against.Null(lockoutEnabled, nameof(lockoutEnabled), "Lockout Eligibility cannot be null");
    }

    public void ChangePassword(string hashedPassword)
    {
        HashedPassword = Guard.Against.NullOrEmpty(hashedPassword, nameof(hashedPassword), "Password cannot be null or empty");
    }

    public void Lock()
    {
        if (LockoutEnabled)
            Locked = true;
    }

    public void Unlock()
    {
        Locked = false;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserName;
        yield return HashedPassword;
    }
}
