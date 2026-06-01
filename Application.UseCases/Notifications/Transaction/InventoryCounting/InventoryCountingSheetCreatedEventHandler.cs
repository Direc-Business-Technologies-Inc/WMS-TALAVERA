using Application.UseCases.Commands.Transaction.InventoryCounting;
using Domain.Entities.Events.Transaction.InventoryCounting;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UseCases.Notifications.Transaction.InventoryCounting;

public class InventoryCountingSheetCreatedEventHandler(
    IServiceProvider serviceProvider)
    : INotificationHandler<InventoryCountingSheetCreatedEvent>
{
    public async Task Handle(InventoryCountingSheetCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Use a new scope since this runs in a fire-and-forget background thread
        // We do this to prevent the parent request's transaction/cancellation token from affecting this separate thread.
        _ = Task.Run(async () =>
        {
            using var scope = serviceProvider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            
            // Fire the sync command independently
            await sender.Send(new SyncInventoryCountingSheetCmd(notification.DocumentId, notification.SheetNo), CancellationToken.None);
        });
        
        await Task.CompletedTask;
    }
}
