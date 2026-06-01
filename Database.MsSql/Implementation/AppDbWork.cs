namespace Database.MsSql.Implementation;

public class AppDbWork<T>() where T : class
{
    protected async Task<TResult> ExecuteAppDbWork<TResult>(Func<Task<TResult>> action)
    {
		try
		{
			var res = await action();
			return res;
		}
		catch (Exception)
		{

			throw;
		}
    }
}
