using Radzen;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class ToastService(
    NotificationService notificationService
    ) : IToastService
{
    public void Error(string message, string? header = null)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = header ?? "Error",
            Detail = message,
            Duration = 4000
        });
    }

    public void Info(string message, string? header = null)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = header ?? "Info",
            Detail = message,
            Duration = 4000
        });
    }

    public void Success(string message, string? header = null)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = header ?? "Success",
            Detail = message,
            Duration = 4000
        });
    }

    public void Warning(string message, string? header = null)
    {
        notificationService.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = header ?? "Warning",
            Detail = message,
            Duration = 4000
        });
    }
}
