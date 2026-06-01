using Ardalis.GuardClauses;
using Domain.Providers;

namespace Domain.Entities.Administration.User.Management;

public class LoginDEM
{
    public int Id { get; private set; }
    public int AttemptCount { get; private set; }
    public bool Succeeded { get; private set; }
    public Guid AccountId { get; private set; }
    public DateTime LoginDate { get; private set; }

    LoginDEM() { }

    public LoginDEM(bool succeeded, Guid accountId)
    {
        LoginDate = DateTimeProvider.UtcNow;
        AttemptCount = 1;
        Succeeded = Guard.Against.Null(succeeded, nameof(succeeded), "Login status cannot be null");
        AccountId = Guard.Against.NullOrEmpty(accountId, nameof(succeeded), "Account Id cannot be null");
    }

    public static LoginDEM Create(bool succeeded, Guid accountId)
    {
        return new(
            succeeded,
            accountId
            );
    }

    public LoginDEM NewAttempt()
    {
        AttemptCount++;
        return this;
    }

    public LoginDEM ResetAttempts()
    {
        AttemptCount = 1;
        return this;
    }

    public LoginDEM ChangeStatus(bool succeeded)
    {
        Succeeded = succeeded;
        return this;
    }

}
