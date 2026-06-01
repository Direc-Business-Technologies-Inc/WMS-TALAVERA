
namespace Shared.Utilities.Repositories;

public interface IAppAction<T>
{
    T? Result { get; }
    bool IsSuccess { get; }
    string? ErrorMessage { get; }
    Exception? Exception { get; }
    AppAction<T> OnSuccess(Func<T, Task> action);
    AppAction<T> OnFailure(Func<Exception, Task> action);
}

public interface IAppAction
{
    bool IsSuccess { get; }
    bool IsCancelled { get; set; }
    string? ErrorMessage { get; }
    Exception? Exception { get; }
    AppAction OnSuccess(Func<Task> action);
    AppAction OnFailure(Func<Exception, Task> action);
}
