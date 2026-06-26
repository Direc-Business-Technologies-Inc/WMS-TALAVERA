using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;

namespace Application.UseCases.Commands.Transaction.Receiving.NS.PurchaseOrder;

public record PostPurchaseOrderCmd(List<PostPurchaseOrderDTO> Data) : ITransactionalRequest<bool>;