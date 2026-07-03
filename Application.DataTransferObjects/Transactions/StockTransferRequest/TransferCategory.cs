using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.StockTransferRequest;

public sealed record TransferCategory
{
    public  int Id { get; init; }
    public  string Name { get; init; }
    public bool IsInterCompany  { get; init; }
    public bool IsReturn { get; init; }

    private TransferCategory(int id, string name, bool isIntercompany, bool isReturn)
    {
        Id = id;
        Name = name;
        IsInterCompany = isIntercompany;
        IsReturn = isReturn;
    }

    public static readonly TransferCategory Transfer = new(1, "Transfer", false, false);
    public static readonly TransferCategory IntercompanyTransfer = new(2, "Intercompany Transfer", true, false);
    public static readonly TransferCategory ReturnsGood = new(3, "Return - Good Items", true, true);
    public static readonly TransferCategory ReturnsBad = new(4, "Return - Bad Items", true, true);

    public static readonly ImmutableArray<TransferCategory> ReturnCategories = [ReturnsGood, ReturnsBad];
    public static readonly ImmutableArray<TransferCategory> Values = [ Transfer, IntercompanyTransfer, ReturnsGood, ReturnsBad];
}
