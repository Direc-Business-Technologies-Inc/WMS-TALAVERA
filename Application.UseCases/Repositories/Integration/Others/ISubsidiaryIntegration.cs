using Application.DataTransferObjects.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ISubsidiaryIntegration
{
    Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId, CancellationToken ct = default);
    Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId, CancellationToken ct = default);
    Task<IEnumerable<SubsidiaryDTO>> GetChildSubsidiariesAsync(int subsidiaryId, CancellationToken ct = default);
    Task<SubsidiaryDTO?> GetSubsidiaryAsync(int subsidiaryid, CancellationToken ct = default);
}
