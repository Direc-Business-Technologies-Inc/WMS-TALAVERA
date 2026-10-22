using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

/// <summary>
///     Resolves the vendor barcode printed under the STR's Vendor field to the
///     vendor on that transfer order.
///     <para>
///         The barcode payload is the vendor NAME (confirmed on the handheld,
///         2026-10-21: scanning the ISABELA GIOS section returned
///         <c>ISABELA GIOS COMMERCIAL, INC.</c>), not a NetSuite internal id. The
///         name is matched against the vendors on the document's own lines, and
///         against the vendor record's own name fields, before the id is used.
///     </para>
///     <para>
///         Scoping to the document matters: it removes the cross-vendor ambiguity a
///         global vendor-master lookup would reintroduce (two vendors can share a
///         display name).
///     </para>
/// </summary>
public record ResolveTransferOrderVendorQry(string OrderNumber, string VendorBarcode)
    : IRequest<ApiResult<VendorLookupDTO>>;

public class ResolveTransferOrderVendorQryHandler(
    ISender sender,
    IVendorIntegration vendorIntegration)
    : IRequestHandler<ResolveTransferOrderVendorQry, ApiResult<VendorLookupDTO>>
{
    public async Task<ApiResult<VendorLookupDTO>> Handle(
        ResolveTransferOrderVendorQry request,
        CancellationToken cancellationToken)
    {
        // Every line on the order: the vendor scan itself is what narrows the set.
        var lines = await sender.Send(
            new GetTransferOrderLineQry(new TransferOrderLineRequestDTO
            {
                OrderNumber = request.OrderNumber
            }),
            cancellationToken)
            ?? [];

        var vendorIds = lines
            .Where(line => line.VendorId is > 0)
            .Select(line => line.VendorId!.Value)
            .Distinct()
            .ToList();

        // The line projection carries the List/Record id plus BUILTIN.DF's display
        // text, which is not guaranteed to be the name the print template rendered.
        // The vendor record's own names (entityid / companyname / fullname) are read
        // for the same ids so any of them can satisfy the scan.
        var vendors = vendorIds.Count == 0
            ? Enumerable.Empty<VendorDTO>()
            : await vendorIntegration.GetVendorsByIdsAsync(vendorIds, cancellationToken);

        return TransferOrderVendorResolver.Resolve(
            lines,
            vendors,
            request.VendorBarcode,
            request.OrderNumber);
    }
}

/// <summary>
///     Pure name-matching for the vendor barcode. Kept separate from the query so
///     the matching rules (and their failure messages) are testable without
///     MediatR or NetSuite.
/// </summary>
public static class TransferOrderVendorResolver
{
    public static ApiResult<VendorLookupDTO> Resolve(
        IEnumerable<TransferOrderLineDTO>? lines,
        IEnumerable<VendorDTO>? vendors,
        string? scannedVendor,
        string orderNumber)
    {
        var scanned = Normalize(scannedVendor, ignorePunctuation: false);

        if (scanned.Length == 0)
        {
            return ApiResult<VendorLookupDTO>.Failed(
                "Scan the vendor barcode printed under the Vendor field.");
        }

        // Every name a printed document could carry for this vendor, grouped by id.
        // The line's BUILTIN.DF text and the record's entityid / companyname /
        // fullname are all candidates: the print template may not have used the
        // same one the line query projects.
        var namesById = new Dictionary<int, List<string>>();

        foreach (var line in lines ?? [])
        {
            Register(namesById, line.VendorId ?? 0, line.VendorName);
        }

        foreach (var vendor in vendors ?? [])
        {
            Register(namesById, vendor.Id, vendor.ReferenceNumber);
            Register(namesById, vendor.Id, vendor.CompanyName);
            Register(namesById, vendor.Id, vendor.Name);
        }

        if (namesById.Count == 0)
        {
            return ApiResult<VendorLookupDTO>.Failed(
                $"Transfer order {orderNumber} has no vendor on its lines.");
        }

        if (namesById.Values.All(names => names.Count == 0))
        {
            return ApiResult<VendorLookupDTO>.Failed(
                $"The vendor names for transfer order {orderNumber} could not be read.");
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
                $"'{scannedVendor?.Trim()}' is not a vendor on transfer order {orderNumber}.{hint}");
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
