using Application.DataTransferObjects.Transactions.SupplierReturn;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;

namespace Web.BlazorServer.Components.Shared.Others;

public partial class BulkFulfillmentNotifications
{
    [Inject] private IBulkFulfillmentNotificationsHandler Handler { get; set; } = default!;
    [Inject] private ILogger<BulkFulfillmentNotifications> Logger { get; set; } = default!;
    private readonly CancellationTokenSource stopping = new();
    private IReadOnlyList<BulkFulfillmentNotificationDTO> Notifications = [];
    private Task? polling;
    private bool Dismissing;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await RefreshAsync();
        polling = PollAsync();
    }

    private async Task PollAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stopping.Token))
                await InvokeAsync(RefreshAsync);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    private async Task RefreshAsync()
    {
        try
        {
            Notifications = await Handler.GetUnreadAsync(stopping.Token);
            StateHasChanged();
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        catch (Exception ex)
        {
            // A refresh failure must not dismiss the currently displayed notifications.
            Logger.LogWarning(ex, "Could not read bulk fulfillment notifications");
        }
    }

    private async Task DismissAsync(BulkFulfillmentNotificationDTO notification)
    {
        Dismissing = true;
        try
        {
            await Handler.DismissAsync(notification.RequestId, notification.Sequence, stopping.Token);
            await RefreshAsync();
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not dismiss bulk fulfillment notification");
            ToastService.Error("The notification could not be dismissed. Please try again.");
        }
        finally { Dismissing = false; }
    }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        if (polling is not null) await polling;
        stopping.Dispose();
    }
}
