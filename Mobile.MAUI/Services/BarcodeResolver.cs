using Shared.Libraries.Entities;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;

namespace Mobile.MAUI.Services;

public enum BarcodeResolutionStatus
{
    Found,
    NotFound,
    Failed
}

/// <summary>
/// Outcome of resolving one scanned value. <see cref="NotFound"/> means NetSuite
/// answered "no such barcode"; <see cref="Failed"/> means the lookup itself did
/// not complete and <see cref="Message"/> carries the real reason. Screens must
/// never render <see cref="Failed"/> as "unknown barcode".
/// </summary>
public readonly record struct BarcodeResolution(
    BarcodeResolutionStatus Status,
    ItemBarcodesPerUoMVM? Barcode,
    string Message)
{
    public bool IsFound => Status == BarcodeResolutionStatus.Found;

    public bool IsFailure => Status == BarcodeResolutionStatus.Failed;

    public static BarcodeResolution Found(ItemBarcodesPerUoMVM barcode) =>
        new(BarcodeResolutionStatus.Found, barcode, string.Empty);

    public static BarcodeResolution NotFound(string scanned) =>
        new(BarcodeResolutionStatus.NotFound, null, $"Unknown barcode: {scanned}");

    public static BarcodeResolution Failed(string message) =>
        new(BarcodeResolutionStatus.Failed, null, message);
}

/// <summary>
/// Resolves a scan against the server's authoritative barcode lookup
/// (<c>POST /Item/PerBarcode</c>). Replaces the per-document barcode list the
/// mobile screens used to preload: that list went stale, could fail silently and
/// only covered the items already on the document.
/// </summary>
public sealed class BarcodeResolver
{
    private readonly ApiClientService Client;
    private readonly Dictionary<string, ItemBarcodesPerUoMVM> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BarcodeResolver(ApiClientService client)
    {
        Client = client;

        // A different endpoint can hold different `customrecord_barcode_per_uom`
        // rows; a cached barcode from the previous host must not resolve here.
        Client.EndpointChanged += Clear;
    }

    /// <summary>
    /// Trims and strips the AIM symbology prefix and control characters a
    /// keyboard-wedge scanner appends. A carton label that "looks correct" can
    /// still carry them, which defeats an exact match.
    /// </summary>
    public static string Normalize(string? scanned)
    {
        if (string.IsNullOrWhiteSpace(scanned))
        {
            return string.Empty;
        }

        var value = scanned.Trim();

        if (value.StartsWith("]C1", StringComparison.OrdinalIgnoreCase))
        {
            value = value[3..];
        }

        return value.Trim('\u0002', '\u0003', '\u0004', '\u001D', ' ', '\r', '\n', '\t');
    }

    public async Task<BarcodeResolution> ResolveAsync(string? scanned)
    {
        var barcode = Normalize(scanned);

        if (barcode.Length == 0)
        {
            return BarcodeResolution.NotFound(scanned?.Trim() ?? string.Empty);
        }

        await _gate.WaitAsync();

        try
        {
            if (_cache.TryGetValue(barcode, out var cached))
            {
                return BarcodeResolution.Found(cached);
            }

            ApiResult<InventoryItemVM>? res;

            try
            {
                res = await Client.Post<InventoryItemVM>("/Item/PerBarcode", new { Barcode = barcode });
            }
            catch (NotAuthorizedException)
            {
                // The shell handles 401 by routing to login; do not mask it here.
                throw;
            }
            catch (Exception e)
            {
                return BarcodeResolution.Failed($"Barcode lookup failed: {e.Message}");
            }

            if (res is null)
            {
                return BarcodeResolution.Failed("Barcode lookup failed: no response from the server.");
            }

            // Body-level 404 comes from the API's own NotFound() result.
            if (res.StatusCode == 404)
            {
                return BarcodeResolution.NotFound(barcode);
            }

            if (!res.Success)
            {
                return BarcodeResolution.Failed($"Barcode lookup failed: {res.ErrorMessage}");
            }

            if (res.Data is null)
            {
                return BarcodeResolution.NotFound(barcode);
            }

            var resolved = new ItemBarcodesPerUoMVM
            {
                NetsuiteMaterialInternalId = res.Data.NetsuiteMaterialInternalId,
                MaterialCode = res.Data.MaterialCode,
                MaterialName = res.Data.MaterialName,
                MaterialWeight = res.Data.MaterialWeight,
                UoMName = res.Data.UoMName,
                UoMRate = res.Data.UoMRate,
                MaterialBarcode = barcode,
            };

            _cache[barcode] = resolved;

            return BarcodeResolution.Found(resolved);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Clear() => _cache.Clear();
}
