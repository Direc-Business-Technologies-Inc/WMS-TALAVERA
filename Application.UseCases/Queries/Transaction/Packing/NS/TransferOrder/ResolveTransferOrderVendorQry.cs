using System.Globalization;
using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

/// <summary>
///     Resolves the vendor barcode printed under the STR's Vendor field to the
///     vendor on that transfer order.
///     <para>
///         Two payloads are accepted. The vendor NAME (confirmed on the handheld,
///         2026-10-21: scanning the ISABELA GIOS section returned
///         <c>ISABELA GIOS COMMERCIAL, INC.</c>) is matched against the document's
///         own line vendors and against the vendor record's name fields
///         (<c>entityid</c>, <c>companyname</c>, <c>fullname</c>). A NetSuite vendor
///         internal id is matched against the document's candidate ids. An id wins
///         when a numeric payload could also be a name.
///     </para>
///     <para>
///         Scoping to the document matters: it removes the cross-vendor ambiguity a
///         global vendor-master lookup would reintroduce (two vendors can share a
///         display name, and an id from another order must not open this gate).
///     </para>
/// </summary>
public record ResolveTransferOrderVendorQry(string OrderNumber, string VendorBarcode)
    : IRequest<ApiResult<VendorLookupDTO>>;

public class ResolveTransferOrderVendorQryHandler(
    INetSuiteApiClientService netSuiteApiClientService,
    IVendorIntegration vendorIntegration)
    : IRequestHandler<ResolveTransferOrderVendorQry, ApiResult<VendorLookupDTO>>
{
    public async Task<ApiResult<VendorLookupDTO>> Handle(
        ResolveTransferOrderVendorQry request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Vendor ids only, distinct over the whole document: this cannot be
            // truncated by the line read's 1,000-row page and transfers a handful
            // of integers instead of every line.
            var candidates = await netSuiteApiClientService.NetsuiteQuery<VendorLookupDTO>(
                "NS_TO_x_Get_Vendor_Candidates",
                new Dictionary<string, string>
                {
                    ["tranid"] = request.OrderNumber
                },
                ct: cancellationToken)
                ?? [];

            var vendorIds = candidates
                .Where(candidate => candidate.Id > 0)
                .Select(candidate => candidate.Id)
                .Distinct()
                .ToList();

            // The candidate script carries ids only; the vendor record's own names
            // (entityid / companyname / fullname) are what a scan is matched against.
            var vendors = vendorIds.Count == 0
                ? Enumerable.Empty<VendorDTO>()
                : await vendorIntegration.GetVendorsByIdsAsync(vendorIds, cancellationToken);

            return TransferOrderVendorResolver.Resolve(
                candidates,
                vendors,
                request.VendorBarcode,
                request.OrderNumber);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ApiResult<VendorLookupDTO>.ServerError(
                $"Vendor lookup for transfer order {request.OrderNumber} failed: {ex.Message}");
        }
    }
}

/// <summary>
///     Pure name-matching for the vendor barcode. Kept separate from the query so
///     the matching rules (and their failure messages) are testable without
///     MediatR or NetSuite.
/// </summary>
public static class TransferOrderVendorResolver
{
    public static List<VendorLookupDTO> GetOptions(
        IEnumerable<VendorLookupDTO>? candidates,
        IEnumerable<VendorDTO>? vendors)
    {
        var candidateIds = (candidates ?? [])
            .Where(candidate => candidate.Id > 0)
            .Select(candidate => candidate.Id)
            .ToHashSet();

        return (vendors ?? [])
            .Where(vendor => candidateIds.Contains(vendor.Id))
            .GroupBy(vendor => vendor.Id)
            .Select(group =>
            {
                var vendor = group.First();
                var name = new[] { vendor.Name, vendor.CompanyName, vendor.ReferenceNumber }
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

                return new VendorLookupDTO
                {
                    Id = vendor.Id,
                    Name = name?.Trim() ?? string.Empty
                };
            })
            .Where(vendor => !string.IsNullOrWhiteSpace(vendor.Name))
            .OrderBy(vendor => vendor.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static ApiResult<VendorLookupDTO> Resolve(
        IEnumerable<VendorLookupDTO>? candidates,
        IEnumerable<VendorDTO>? vendors,
        string? scannedVendor,
        string orderNumber)
    {
        var scanned = Normalize(scannedVendor, ignorePunctuation: false);

        if (scanned.Length == 0)
        {
            return ApiResult<VendorLookupDTO>.Failed(
                "Scan the vendor barcode printed under the Vendor field — the vendor ID or vendor name.");
        }

        // The document's own vendor ids seed the map; the record's entityid /
        // companyname / fullname are the names a scan can match, because the
        // print template may not use the same value the line carried.
        var namesById = new Dictionary<int, List<string>>();

        foreach (var candidate in candidates ?? [])
        {
            Register(namesById, candidate.Id, null);
        }

        foreach (var vendor in vendors ?? [])
        {
            if (!namesById.ContainsKey(vendor.Id))
            {
                continue;
            }

            Register(namesById, vendor.Id, vendor.ReferenceNumber);
            Register(namesById, vendor.Id, vendor.CompanyName);
            Register(namesById, vendor.Id, vendor.Name);
        }

        if (namesById.Count == 0)
        {
            return ApiResult<VendorLookupDTO>.Failed(
                $"Transfer order {orderNumber} has no vendor on its lines. " +
                "Fulfil this order from web Packing instead.");
        }

        // Internal-id channel: the label may carry the NetSuite vendor internal id
        // instead of a name. Only ids that are already candidates on this document
        // are accepted, so a scan cannot open the gate on another order's vendor.
        // Checked before the name channels so a numeric payload that is both a
        // valid id and a numeric vendor name resolves deterministically to the id.
        if (int.TryParse(
                scannedVendor?.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var scannedId)
            && scannedId > 0
            && namesById.ContainsKey(scannedId))
        {
            return ApiResult<VendorLookupDTO>.Succeeded(new VendorLookupDTO
            {
                Id = scannedId,
                Name = namesById[scannedId].FirstOrDefault()
                       ?? scannedId.ToString(CultureInfo.InvariantCulture)
            });
        }

        if (namesById.Values.All(names => names.Count == 0))
        {
            return ApiResult<VendorLookupDTO>.Failed(
                $"The vendor names for transfer order {orderNumber} could not be read. " +
                "Scan the vendor ID instead.");
        }

        // Strict first (whitespace + case), then a punctuation-insensitive retry:
        // the printed label and the record name can differ by a comma or a period.
        var matchedIds = Match(namesById, scannedVendor, ignorePunctuation: false);

        if (matchedIds.Count == 0)
        {
            matchedIds = Match(namesById, scannedVendor, ignorePunctuation: true);
        }

        if (matchedIds.Count == 1)
        {
            var id = matchedIds[0];

            return ApiResult<VendorLookupDTO>.Succeeded(new VendorLookupDTO
            {
                Id = id,
                Name = DisplayName(namesById[id], scannedVendor)
            });
        }

        if (matchedIds.Count == 0)
        {
            var known = namesById.Values
                .SelectMany(names => names)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            var hint = known.Count > 0
                ? $" Vendors on this order: {string.Join("; ", known)}."
                : string.Empty;

            return ApiResult<VendorLookupDTO>.Failed(
                $"'{scannedVendor?.Trim()}' is not a vendor ID or vendor name on transfer order {orderNumber}.{hint}");
        }

        return ApiResult<VendorLookupDTO>.Failed(
            $"'{scannedVendor?.Trim()}' matches more than one vendor on transfer order {orderNumber}.");
    }

    private static void Register(Dictionary<int, List<string>> namesById, int vendorId, string? name)
    {
        if (vendorId <= 0)
        {
            return;
        }

        if (!namesById.TryGetValue(vendorId, out var names))
        {
            namesById[vendorId] = names = [];
        }

        var trimmed = name?.Trim();

        if (!string.IsNullOrWhiteSpace(trimmed)
            && !names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            names.Add(trimmed);
        }
    }

    private static List<int> Match(
        Dictionary<int, List<string>> namesById,
        string? scannedVendor,
        bool ignorePunctuation)
    {
        // The scan must be normalized with the same rules as the candidate names;
        // otherwise the loose pass compares a punctuation-free vendor name against
        // a still-punctuated scan and never matches.
        var key = Normalize(scannedVendor, ignorePunctuation);

        return namesById
            .Where(pair => pair.Value.Any(name => Normalize(name, ignorePunctuation) == key))
            .Select(pair => pair.Key)
            .ToList();
    }

    private static string DisplayName(List<string> names, string? scannedVendor)
    {
        var strict = Normalize(scannedVendor, ignorePunctuation: false);
        var loose = Normalize(scannedVendor, ignorePunctuation: true);

        return names.FirstOrDefault(name => Normalize(name, false) == strict)
            ?? names.FirstOrDefault(name => Normalize(name, true) == loose)
            ?? names.FirstOrDefault()
            ?? scannedVendor?.Trim()
            ?? string.Empty;
    }

    private static string Normalize(string? value, bool ignorePunctuation)
    {
        var collapsed = string.Join(
                ' ',
                (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();

        if (!ignorePunctuation)
        {
            return collapsed;
        }

        return collapsed
            .Replace(",", string.Empty)
            .Replace(".", string.Empty)
            .Trim();
    }
}
