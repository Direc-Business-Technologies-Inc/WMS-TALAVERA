using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Packing.STR;

public sealed record TransferCategoryPacking
{
    public  int Id { get; init; }
    public  string Name { get; init; }
    public bool IsInterCompany  { get; init; }
    public bool IsReturn { get; init; }

    private TransferCategoryPacking(int id, string name, bool isIntercompany, bool isReturn)
    {
        Id = id;
        Name = name;
        IsInterCompany = isIntercompany;
        IsReturn = isReturn;
    }

    public static readonly TransferCategoryPacking Transfer = new(1, "Transfer", false, false);
    public static readonly TransferCategoryPacking IntercompanyTransfer = new(2, "Intercompany Transfer", true, false);
    public static readonly TransferCategoryPacking ReturnsGood = new(3, "Return - Good Items", true, true);
    public static readonly TransferCategoryPacking ReturnsBad = new(4, "Return - Bad Items", true, true);

    public static readonly ImmutableArray<TransferCategoryPacking> ReturnCategories = [ReturnsGood, ReturnsBad];
    public static readonly ImmutableArray<TransferCategoryPacking> Values = [ Transfer, IntercompanyTransfer, ReturnsGood, ReturnsBad];
}
