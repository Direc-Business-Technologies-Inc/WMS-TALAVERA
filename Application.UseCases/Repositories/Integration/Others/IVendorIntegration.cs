using Application.DataTransferObjects.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IVendorIntegration
{
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsListAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default);
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default);
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default);

    /// <summary>
    ///     Reads the vendor records for a known set of internal ids. Used to resolve
    ///     a scanned vendor name (the barcode payload) against the name fields a
    ///     printed document can carry: <c>entityid</c>, <c>companyname</c>, and
    ///     <c>fullname</c>.
    /// </summary>
    Task<IEnumerable<VendorDTO>> GetVendorsByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
