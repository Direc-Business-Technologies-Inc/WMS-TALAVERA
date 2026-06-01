using Shared.Utilities.Repositories;

namespace Shared.Utilities;

public class AppAction : IAppAction
{
    public AppActionFactoryOptions Options { get; init; }
    public bool IsSuccess => Exception == null && !IsCancelled;
    public bool IsCancelled { get; set; } = false;
    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }

    public void SetException(Exception ex)
    {
        Exception = ex;
        ErrorMessage = ex.Message;
    }

    public AppAction OnSuccess(Func<Task> action)
    {
        if (IsSuccess)
        {
            _ = action();
        }
        return this;
    }

    public AppAction OnFailure(Func<Exception, Task> action)
    {
        if (!IsSuccess)
        {
            _ = action(Exception);
        }
        return this;
    }
}

public class AppAction<T> : IAppAction<T>
{
    public AppActionFactoryOptions Options { get; init; }
    public T? Result { get; private set; }
    public bool IsSuccess => Exception == null && !IsCancelled;
    public bool IsCancelled { get; set; } = false;
    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }

    public void SetResult(T? result) => Result = result;
    public void SetException(Exception ex)
    {
        Exception = ex;
        ErrorMessage = ex.Message;
    }

    public AppAction<T> OnSuccess(Func<T, Task> action)
    {
        if (IsSuccess && Result != null)
        {
            _ = action(Result);
        }
        return this;
    }

    public AppAction<T> OnFailure(Func<Exception, Task> action)
    {
        if (!IsSuccess && Exception != null)
        {
            _ = action(Exception);
        }
        return this;
    }
}


public class AppActionFactoryOptions
{
    public required string ActionName { get; init; } = string.Empty;
    public bool ShowLoadingIndicator { get; set; } = true;
    public bool ShowConfirmationDialog { get; set; } = true;
    public bool ShowToastOnSuccess { get; set; } = true;
    public bool ShowToastOnFailure { get; set; } = true;
}
