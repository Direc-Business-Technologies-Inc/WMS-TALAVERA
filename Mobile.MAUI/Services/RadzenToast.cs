using Mobile.MAUI.Interfaces;
using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Services;

internal class RadzenToast : IToast
{
    private readonly NotificationService _notifService;

    public RadzenToast(NotificationService notifService)
    {
        _notifService = notifService;
    }

    public void Clear()
    {
        _notifService.Messages.Clear();
    }

    public void Dispose()
    {
        // dispose if needed
    }

    public void Error(string message)
    {
        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Error(string message, string title)
    {
        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Error,
            Summary = title,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Info(string message)
    {
        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Info(string message, string title)
    {
        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Info,
            Summary = title,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Success(string message)
    {

        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Success(string message, string title)
    {

        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = title,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Warning(string message)
    {
        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }

    public void Warning(string message, string title)
    {
        var msg = new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = title,
            Detail = message,
            Duration = 5000
        };
        _notifService.Notify(msg);
    }
}
