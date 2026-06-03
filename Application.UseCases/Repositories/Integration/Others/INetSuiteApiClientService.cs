using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface INetSuiteApiClientService : INotifyPropertyChanged
{
    Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null);
    Task<IEnumerable<T>?> NetsuiteQuery<T>(string queryName, Dictionary<string, string>? parameters = null, int limit = 0, int offset = 0);
}
