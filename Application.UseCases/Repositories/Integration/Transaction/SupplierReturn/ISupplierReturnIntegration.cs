using Application.DataTransferObjects.Transactions.Receiving;
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
    Task<SupplierReturnDTO?> GetReturnAsync(string referenceNumber);
    Task<IEnumerable<SupplierReturnLineDTO>> GetReturnLinesAsync(string referenceNumber);
    Task<bool> CreateSupplierReturn(SupplierReturnDTO data);
    Task<bool> UpdateSupplierReturn(SupplierReturnDTO data);
    Task<bool> SubmitSupplierReturnForApproval(SupplierReturnDTO data);
    Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent);
    Task<SupplierReturnDTO?> GetReturnFromPurchaseOrderAsync(string purchaseOrderId);
    Task<IEnumerable<SupplierReturnLineDTO>> GetReturnFromPurchaseOrderLinesAsync(string purchaseOrderId);
    Task<(IEnumerable<PurchaseSubCategoryDTO>, int count)> GetPurchaseSubcategoriesAsync(DataGridIntent intent);
    Task<(IEnumerable<PurchaseCategoryDTO>, int count)> GetPurchaseCategoriesAsync(DataGridIntent intent);

}
