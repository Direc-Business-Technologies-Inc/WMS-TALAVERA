using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Integration.Transaction.InventoryCounting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class InventoryCountingIntegration : IInventoryCountingIntegration
{
    public Task<bool> PostInventoryCountings(InventoryCountingDocumentDTO data)
    {
        throw new NotImplementedException();
    }
}
