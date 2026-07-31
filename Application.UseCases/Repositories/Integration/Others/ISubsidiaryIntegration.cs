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
    Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent);
    Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId);
    Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId);
    Task<IEnumerable<SubsidiaryDTO>> GetChildSubsidiariesAsync(int subsidiaryId);
    Task<SubsidiaryDTO?> GetSubsidiaryAsync(int subsidiaryid);
}
