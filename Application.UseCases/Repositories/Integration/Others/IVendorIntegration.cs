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
}
