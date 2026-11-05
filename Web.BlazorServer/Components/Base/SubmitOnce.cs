namespace Web.BlazorServer.Components.Base;

/// <summary>
/// Prevents concurrent execution of a web form submission.
/// </summary>
public sealed class SubmitOnce
{
    private int _isSubmitting;

    public bool IsSubmitting => Volatile.Read(ref _isSubmitting) == 1;

    public async Task<bool> RunAsync(Func<Task> submit)
    {
        ArgumentNullException.ThrowIfNull(submit);

        if (Interlocked.CompareExchange(ref _isSubmitting, 1, 0) != 0)
            return false;

        try
        {
            await submit();
            return true;
        }
        finally
        {
            Volatile.Write(ref _isSubmitting, 0);
        }
    }
}
