using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Vestigial
{
    internal class TransactionTypeIntegration : ITransactionTypeIntegration
    {
        public Task<IEnumerable<TransactionTypeSAPDTO>> GetTransactionTypesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
