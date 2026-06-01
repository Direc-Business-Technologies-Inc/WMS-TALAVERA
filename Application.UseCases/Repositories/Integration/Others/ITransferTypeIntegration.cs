using Application.DataTransferObjects.Transactions.InventoryTransfer.SAP;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ITransferTypeIntegration
{
    Task<IEnumerable<TransferTypeSAPDTO>> GetTransferTypesAsync();
}

