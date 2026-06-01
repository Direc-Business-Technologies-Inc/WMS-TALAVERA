namespace Domain.Providers;

public static class DateTimeProvider
{
    public static DateTime UtcNow => DateTime.UtcNow;
    public static DateTime UtcToday => DateTime.Today.ToUniversalTime();
    public static DateTime Now => DateTime.Now;
    public static DateTime Today => DateTime.Today;
}
