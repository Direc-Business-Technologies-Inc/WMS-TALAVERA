using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.UseCases.Repositories.Integration.Others;
using MediatR;
using Shared.Libraries.Entities;

namespace Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;

public record GetTransferOrderVendorsQry(string OrderNumber)
    : IRequest<ApiResult<IEnumerable<VendorLookupDTO>>>;

public class GetTransferOrderVendorsQryHandler(
    INetSuiteApiClientService netSuiteApiClientService,
    IVendorIntegration vendorIntegration)
    : IRequestHandler<GetTransferOrderVendorsQry, ApiResult<IEnumerable<VendorLookupDTO>>>
{
    public async Task<ApiResult<IEnumerable<VendorLookupDTO>>> Handle(
        GetTransferOrderVendorsQry request,
        CancellationToken cancellationToken)
    {
        try
        {
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

            var vendors = vendorIds.Count == 0
                ? Enumerable.Empty<VendorDTO>()
                : await vendorIntegration.GetVendorsByIdsAsync(vendorIds, cancellationToken);

            return ApiResult<IEnumerable<VendorLookupDTO>>.Succeeded(
                TransferOrderVendorResolver.GetOptions(candidates, vendors));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ApiResult<IEnumerable<VendorLookupDTO>>.ServerError(
                $"Vendor options for transfer order {request.OrderNumber} failed: {ex.Message}");
        }
    }
}
