using Domain.Entities.System;
using Domain.Entities.Transaction.Common;

namespace Database.MsSql.Configurations;

public static class AppDocuments
{
    static int DocCode = 100000;
    public static readonly IEnumerable<DocumentTypeDEM> List =
    [
        DocumentTypeDEM.Create(++DocCode , "User"),
        DocumentTypeDEM.Create(++DocCode , "Receiving"),
        DocumentTypeDEM.Create(++DocCode , "Goods Return"),
        DocumentTypeDEM.Create(++DocCode , "Delivery"),
        DocumentTypeDEM.Create(++DocCode , "Sales Return"),
        DocumentTypeDEM.Create(++DocCode , "Goods Issue"),
        DocumentTypeDEM.Create(++DocCode , "Goods Receipt"),
        DocumentTypeDEM.Create(++DocCode , "Inventory Transfer"),
        DocumentTypeDEM.Create(++DocCode , "Inventory Counting"),
        DocumentTypeDEM.Create(++DocCode , "Counting Sheet"),
    ];

    public static readonly IEnumerable<DocumentNumberDEM> Series =
    [
        DocumentNumberDEM.Create(List.ElementAt(0).Id, "WM", "S"),
        DocumentNumberDEM.Create(List.ElementAt(1).Id, "WMS-", "Receiving#"),
        DocumentNumberDEM.Create(List.ElementAt(2).Id, "WMS-", "GoodsReturn#"),
        DocumentNumberDEM.Create(List.ElementAt(3).Id, "WMS-", "Delivery#"),
        DocumentNumberDEM.Create(List.ElementAt(4).Id, "WMS-", "SalesReturn#"),
        DocumentNumberDEM.Create(List.ElementAt(5).Id, "WMS-", "GoodsIssue#"),
        DocumentNumberDEM.Create(List.ElementAt(6).Id, "WMS-", "GoodsReceipt#"),
        DocumentNumberDEM.Create(List.ElementAt(7).Id, "WMS-", "InventoryTransfer#"),
        DocumentNumberDEM.Create(List.ElementAt(8).Id, "WMS-", "InventoryCount#"),
        DocumentNumberDEM.Create(List.ElementAt(9).Id, "WMS-", "CountSheet#"),
    ];
}
