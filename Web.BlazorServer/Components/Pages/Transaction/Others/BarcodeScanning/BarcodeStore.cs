using Mapster;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Others.BarcodeScanning;

public class BarcodeStore
{
    private Dictionary<string, BarcodeStoreItem> _items = new();

    public void AddBarcode(BarcodeVM barcode)
    {
        if (!_items.ContainsKey(barcode.Barcode)) _items[barcode.Barcode] = new(barcode);

        _items[barcode.Barcode].Count++;
    }
    public decimal CountItemQuantity(int itemId) => _items.Values
        .Where(x => x.Item?.Id == itemId)
        .Sum(x => x.Count * (x.UoM?.ConversionRate ?? 0));
    public decimal CountItemQuantity(ItemsVM item) => CountItemQuantity(item.Id);
    public int GetBarcodeCount(BarcodeVM barcode) => GetBarcodeCount(barcode.Barcode);
    public int GetBarcodeCount(string barcode) => _items[barcode]?.Count ?? 0;
    public bool Contains(BarcodeVM barcode) => _items.ContainsKey(barcode.Barcode);
    public bool Contains(string barcode) => _items.ContainsKey(barcode);
    public void Clear() => _items.Clear();
    public bool Any() => _items.Any();
    public IEnumerable<BarcodeVM> Barcodes => _items.Values.Select(x => x.Barcode);
    public IEnumerable<ItemsVM> Items => _items.Values.Where(x => x.Item != null).DistinctBy(x => x.Barcode.Item!.Id).Select(x => x.Item!);
    public class BarcodeStoreItem(BarcodeVM barcode)
    {
        public BarcodeVM Barcode { get; init; } = barcode;
        public int Count { get; set; } = 0;
        public ItemsVM? Item => Barcode.Item;
        public ItemUnitVM? UoM => Barcode.UoM;
    }
}
