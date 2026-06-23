using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving;
﻿using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using System.ComponentModel;

namespace Application.UseCases.Repositories.Integration.Others;

public interface INetSuiteApiClientService : INotifyPropertyChanged
{
    Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null);
    Task<IEnumerable<T>?> NetsuiteQuery<T>(string queryName, Dictionary<string, string>? parameters = null, int limit = 0, int offset = 0);
    Task<T> MakeRequest<T>(string url, string? reqBody, HttpMethod method);
    Task<T> MakeRequestOAuth1<T>(string url, string? reqBody);


    Task<bool> SavePOItemReceipt(List<PostPurchaseOrderDTO> Data);
    Task<bool> SaveTOItemReceipt(List<PostTransferOrderDTO> Data);
    Task<bool> SaveReturnsItemReceipt(List<PostReturnsDTO> Data);
    
    string GetRestAPIURI { get; }
    string GetRestletURI { get; }


    Task<bool> SaveTOItemFulfillment(List<PostTransferOrderDTO> Data);
    Task<bool> SaveReturnsItemFulfillment(List<PostReturnsDTO> Data);
    Task<bool> SaveVRAItemFulfillment(List<PostVendorReturnAuthorizationDTO> Data);

    Task<bool> SaveTripTicket(PostTripTicketDTO Data);
}
