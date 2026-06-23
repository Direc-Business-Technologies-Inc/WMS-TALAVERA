using Application.DataTransferObjects.Transactions.InventoryTransfer.SAP;
using Application.UseCases.Repositories.Integration.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Vestigial
{
    internal class TransferTypeIntegration : ITransferTypeIntegration
    {
        public Task<IEnumerable<TransferTypeSAPDTO>> GetTransferTypesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
