using Mapster;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;


namespace Web.BlazorServer.Handlers.Implementations.Transaction.StockTransferRequest;

public class StockTransferRequestMockHandler : IStockTransferRequestHandler
{
    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetStockTransferRequestsList(DataGridIntent intent)
    {
        return (STR_BANK.Skip(intent.Skip).Take(intent.Take).Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), STR_BANK.Count);
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetTransferOrdersList(DataGridIntent intent)
    {
        var sublist = STR_BANK.Where(x => x.Status.Equals("Transfer Order"));
        return (sublist.Skip(intent.Skip).Take(intent.Take).Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), sublist.Count());
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetInterCompanyTransferOrdersList(DataGridIntent intent)
    {
        var sublist = STR_BANK.Where(x => x.Status.Equals("ICTO"));
        return (sublist.Skip(intent.Skip).Take(intent.Take).Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), sublist.Count());
    }

    public async Task<(IEnumerable<StockTransferRequestDataGridVM> data, int count)> GetReturnsList(DataGridIntent intent)
    {
        var sublist = STR_BANK.Where(x => x.Status.Equals("Returns"));
        return (sublist.Skip(intent.Skip).Take(intent.Take).Adapt<IEnumerable<StockTransferRequestDataGridVM>>(), sublist.Count());
    }

    public async Task<(IEnumerable<StockTransferRequestLineVM> data, int count)> GetStockTransferRequestLines(string reference, DataGridIntent intent)
    {
        var str = STR_BANK.FirstOrDefault(x => x.ReferenceNumber.Equals(reference, StringComparison.OrdinalIgnoreCase));
        if (str == null) throw new Exception("stock transfer request dne");
        return (str.Lines.Skip(intent.Skip).Take(intent.Take), 0);
    }

    public async Task<StockTransferRequestInfoVM?> GetStockTransferRequest(string reference, bool includeLines = true)
    {
        var str = STR_BANK.FirstOrDefault(x => x.ReferenceNumber.Equals(reference, StringComparison.OrdinalIgnoreCase));
        if (str == null) return null;
        return new StockTransferRequestInfoVM()
        {
            Id = str.Id,
            ReferenceNumber = str.ReferenceNumber,
            Requestor = str.Requestor,
            SourceLocation = str.SourceLocation,
            DestinationLocation = str.DestinationLocation,
            Subsidiary = str.Subsidiary,
            Remarks = str.Remarks,
            Date = str.Date,
            Lines = includeLines ? str.Lines : []
        };
    }

    private const int LINES_COUNT = 15;
    private const int STR_COUNT = 100;
    private const int ITEMS_COUNT = 30;

    public StockTransferRequestMockHandler()
    {
        if (ITEMS_BANK.Count == 0) _generateItems();
        if (STR_BANK.Count == 0) _generateSTR();
    }

    private void _generateItems()
    {
        for (int i = 0; i < ITEMS_COUNT; ++i)
        {
            ITEMS_BANK.Add(($"ITEM{i:000}", $"Mock item #{i}", Random.Shared.Next(1500)));
        }
    }

    private void _generateSTR()
    {

        for (int i = 0; i < STR_COUNT; ++i)
        {
            var x = new StockTransferRequestInfoVM()
            {
                Id = i,
                ReferenceNumber = $"STR{i:000}",
                Status = Random.Shared.Next(3) switch { 0 => "Transfer Order", 1 => "ICTO", _ => "Returns"},
                Requestor = $"Some Guy",
                SourceLocation = $"Penn State",
                DestinationLocation = $"Nebraska",
                Subsidiary = $"Some Other Guy",
                Remarks = "this is not a real item",
                Date = _randomDate()
            };

            for (int j = 0; j < LINES_COUNT; ++j)
            {
                if (Random.Shared.NextDouble() < 0.8)
                {
                    var item = ITEMS_BANK[Random.Shared.Next(ITEMS_BANK.Count)];
                    x.Lines.Add(new StockTransferRequestLineVM()
                    {
                        ItemCode = item.code,
                        ItemDescription = item.desc,
                        UoM = "in bags of 32 divided amongst 15 people",
                        Warehouse = "The land of sunshine",
                        QuantityOnHand = item.quantity,
                        QuantityAlloted = 69
                    });
                }
            }
            STR_BANK.Add(x);
        }
    }

    private DateTime _randomDate()
    {

        DateTime end = DateTime.Now;
        DateTime start = end.AddMonths(-1);

        TimeSpan range = end - start;

        long randomSeconds = Random.Shared.NextInt64((long)range.TotalMinutes);

        // Add the random seconds to the start date
        return start.AddMinutes(randomSeconds);
    }


    private static List<(string code, string desc, int quantity)> ITEMS_BANK = [];
    private static List<StockTransferRequestInfoVM> STR_BANK = [];

}