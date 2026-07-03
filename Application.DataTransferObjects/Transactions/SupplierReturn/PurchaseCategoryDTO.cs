using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.SupplierReturn;

public class PurchaseCategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PurchaseSubCategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PurchaseCategoryId { get; set; }
}