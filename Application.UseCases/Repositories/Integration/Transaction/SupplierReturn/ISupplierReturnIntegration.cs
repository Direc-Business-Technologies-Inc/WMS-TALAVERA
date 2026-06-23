using Application.DataTransferObjects.Transactions.SupplierReturn;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;

public interface ISupplierReturnIntegration
{
    Task<(IEnumerable<SupplierReturnDataGridDTO> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ReturnCategoryDTO> Data, int Count)> GetReturnCategories(DataGridIntent intent);
    Task<(IEnumerable<ReturnStatusDTO> Data, int Count)> GetReturnStatuses(DataGridIntent intent);
    Task<SupplierReturnDTO?> GetReturnAsync();
    Task<IEnumerable<SupplierReturnLineDTO>> GetReturnLinesAsync();
}
