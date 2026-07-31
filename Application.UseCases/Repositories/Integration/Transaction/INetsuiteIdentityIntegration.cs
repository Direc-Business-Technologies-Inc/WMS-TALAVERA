using Application.DataTransferObjects.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Transaction;

public interface INetsuiteIdentityIntegration
{
    Task<NetsuiteIdentityDTO?> GetNetsuiteIdentityAsync(int netsuiteEmployeeId);
}
