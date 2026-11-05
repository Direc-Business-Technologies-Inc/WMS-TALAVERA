namespace Mobile.MAUI.Services;

public class NotAuthorizedException : Exception
{
    public NotAuthorizedException()
        : base("Your session has expired. Please log in again.")
    {
    }

    public NotAuthorizedException(string message)
        : base(message)
    {
    }
}
