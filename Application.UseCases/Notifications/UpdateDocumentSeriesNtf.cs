using Application.UseCases.Commands.System.DocumentSeries;
using MediatR;

namespace Application.UseCases.Notifications;

public record UpdateDocumentSeriesNtf(Guid DocumentTypeId) : INotification;

public class UpdateDocumentSeriesNtfHandler(
    ISender Sender)
    : INotificationHandler<UpdateDocumentSeriesNtf>
{
    public async Task Handle(UpdateDocumentSeriesNtf notification, CancellationToken cancellationToken)
    {
        await Sender.Send(new UpdateTransactionalDocumentSeriesByDocumentTypeCmd(notification.DocumentTypeId), cancellationToken);
    }
}
