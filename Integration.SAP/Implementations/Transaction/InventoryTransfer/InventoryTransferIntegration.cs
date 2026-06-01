using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransfer;
using B1SLayer;
using Database.Libraries.Repositories;
using Integration.Sap.Entities;
using Integration.Sap.Helpers;
using Integration.Sap.Repositories;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Shared.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Integration.SAP.Implementations.Transaction.InventoryTransfer;

public partial class InventoryTransferIntegration(
    ISqlQueryManager qryManager,
    IServiceLayerActions SLActions) : IInventoryTransferIntegration
{
    readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
    };

    const string ITR_NOT_FOUND_MSG = "Could not find inventory transfer request with DocEntry={0}";
    const string PENDING_ITR_NOT_FOUND_MSG = "Could not find pending inventory transfer with DraftEntry={0}";
    const string REJECTED_ITR_NOT_FOUND_MSG = "Could not find rejected inventory transfer with DraftEntry={0}";
    const string POSTED_ITR_NOT_FOUND_MSG = "Could not find posted inventory transfer with DocEntry={0}";
    const string ITR_NOT_POSTED_MSG = "The inventory transfer request could not be created";

    #region Lists
    public async Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "ToWhsName", "T1.WhsName" },
                { "FromWhsName", "T2.WhsName" },
                { "Remarks", "T0.U_Remarks" },
                { "PreparedBy", "T0.U_PrepBy" },
            };
        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata(
            "APHI_InventoryTransfer_RequestDataGrid",
            out string qry,
            out bool found
        );

        if (!found)
            throw new Exception("Query for getting all open Inventory Transfer Request not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<InventoryTransferDataGridSAPDTO> docs = await SLActions.RawQueryAsync<InventoryTransferDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }
    public async Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetPendingInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "ToWhsName", "T1.WhsName" },
                { "FromWhsName", "T2.WhsName" },
                { "Remarks", "T0.U_Remarks" },
                { "PreparedBy", "T0.U_PrepBy" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_InventoryTransfer_PendingRequestDataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all pending Inventory Transfer Request not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<InventoryTransferDataGridSAPDTO> docs = await SLActions.RawQueryAsync<InventoryTransferDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }
    public async Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetRejectedInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "ToWhsName", "T1.WhsName" },
                { "FromWhsName", "T2.WhsName" },
                { "Remarks", "T0.U_Remarks" },
                { "PreparedBy", "T0.U_PrepBy" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_InventoryTransfer_RejectedRequestDataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all rejected Inventory Transfer Request not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<InventoryTransferDataGridSAPDTO> docs = await SLActions.RawQueryAsync<InventoryTransferDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }
    public async Task<(IEnumerable<InventoryTransferDataGridSAPDTO>, int)> GetPostedInventoryTransferRequestListAsync(DataGridIntent intent)
    {
        Dictionary<string, string> columnMap = new()
            {
                { "DocEntry", "T0.DocEntry" },
                { "DocNum", "T0.DocNum" },
                { "DocDate", "T0.DocDate" },
                { "ToWhsName", "T1.WhsName" },
                { "FromWhsName", "T2.WhsName" },
                { "Remarks", "T0.U_Remarks" },
                { "PreparedBy", "T0.U_PrepBy" },
            };

        if (intent.Sorts.Count <= 0)
        {
            intent.Sorts.Add(new AppSortDescriptor
            {
                Property = "DocDate",
                Direction = SortDirectionEnum.Descending
            });
        }

        var qryDetails = qryManager.GetSqlScriptWithMetadata("APHI_InventoryTransfer_PostedRequestDataGrid", out string qry, out bool found);
        if (!found)
            throw new Exception("Query for getting all open Inventory Transfer Request not found.");

        string query = DataGridQueryBuilder.BuildQuery(qry, intent);
        string countQuery = DataGridQueryBuilder.BuildCountQuery(qry, intent.Filters, columnMap);

        List<InventoryTransferDataGridSAPDTO> docs = await SLActions.RawQueryAsync<InventoryTransferDataGridSAPDTO>(query);
        TotalRows? rowCount = await SLActions.RawQueryOneAsync<TotalRows>(countQuery);

        return (docs, rowCount?.Count ?? docs.Count);
    }

    #endregion

    #region Headers
    public async Task<InventoryTransferRequestHeaderSAPDTO?> GetInventoryTransferRequestHeaderAsync(int DocEntry)
    {
        var result = await SLActions.SingleAsync<InventoryTransferRequestHeaderSAPDTO, object>(
            "APHI_InventoryTransfer_RequestHeader",
            new { DocEntry }
        );
        if (result == null)
            throw new FileNotFoundException(string.Format(ITR_NOT_FOUND_MSG, DocEntry));
        
        return result;
    }
    public async Task<InventoryTransferHeaderSAPDTO?> GetInventoryTransferRequestDraftHeaderAsync(int docEntry, string status)
    {
        InventoryTransferHeaderSAPDTO? result = await SLActions.SingleAsync<InventoryTransferHeaderSAPDTO, object>("APHI_InventoryTransfer_DraftRequestHeader", new { docEntry, status });

        if (result == null)
        {
            throw new FileNotFoundException(
                string.Format(status == "N" ? REJECTED_ITR_NOT_FOUND_MSG : PENDING_ITR_NOT_FOUND_MSG, docEntry)
           );
        }

        return result;
    }
    public async Task<InventoryTransferHeaderSAPDTO?> GetPostedInventoryTransferRequestHeaderAsync(int docEntry)
    {
        InventoryTransferHeaderSAPDTO? result = await SLActions.SingleAsync<InventoryTransferHeaderSAPDTO, object>("APHI_InventoryTransfer_PostedRequestHeader", new { docEntry });

        if (result == null)
            throw new FileNotFoundException(string.Format(POSTED_ITR_NOT_FOUND_MSG, docEntry));

        return result;
    }

    #endregion

    #region Lines
    public async Task<IEnumerable<InventoryTransferRequestLineSAPDTO>> GetInventoryTransferRequestLinesAsync(int DocEntry)
    {
        return await SLActions.QueryAsync<InventoryTransferRequestLineSAPDTO, object>(
            "APHI_InventoryTransfer_RequestLines",
            new { DocEntry }
        );
    }
    public async Task<IEnumerable<InventoryTransferLineSAPDTO>> GetInventoryTransferRequestDraftLinesAsync(int docEntry)
    {
        List<InventoryTransferLineSAPDTO>? lines = await SLActions.QueryAsync<InventoryTransferLineSAPDTO, object>("APHI_InventoryTransfer_DraftRequestLines", new { docEntry });

        return lines;
    }
    public async Task<IEnumerable<InventoryTransferLineSAPDTO>> GetPostedInventoryTransferRequestLinesAsync(int docEntry)
    {

        List<InventoryTransferLineSAPDTO>? lines = await SLActions.QueryAsync<InventoryTransferLineSAPDTO, object>("APHI_InventoryTransfer_PostedRequestLines", new { docEntry });

        return lines;
    }

    #endregion
    public async Task<int> PostInventoryTransferRequest(InventoryTransferRequestDTO data)
    {
        List<InventoryTransferLinesPayload> linesPayload = [];
        foreach (var line in data.Lines.Where(line => line.AllotedQuantity > 0))
        {
            linesPayload.Add(InventoryTransferLinesPayload.Create(
                quantity: line.AllotedQuantity,
                itemCode: line.ItemCode,
                baseLine: line.LineNum,
                baseEntry: data.DocEntry
            ));
        }

        InventoryTransferPayload payload = InventoryTransferPayload.Create(
            docDate: data.DocDate,
            fromWarehouse: data.FromWarehouse.WhsCode,
            toWarehouse: data.ToWarehouse.WhsCode,
            transferType: data.TransferType?.Code,
            remarks: data.Remarks,
            preparedBy: data.PreparedBy,
            approvedBy: data.ApprovedBy,
            notedBy: data.NotedBy,
            lines: linesPayload,
            docEntry: data.DocEntry,
            docNum: data.DocNum,
            schoolYear: data.SchoolYear.Code
        );

        string jsonString = JsonSerializer.Serialize(payload, jsonSerializerOptions);
        try
        {
            var result = await SLActions.PostAsync<object, InventoryTransferPayload>("StockTransfers", payload);
        }
        catch (SLException ex) when (ex.InnerException is Flurl.Http.FlurlHttpException httpException)
        {
            if (ex.Message.Contains("-2028"))
            {
                try
                {
                    // /StockTransfers sends a 404 error because OWTR entry is NOT created
                    // OWTR entry is not created because of confirmation process, instead it creates ODRF,OWDD
                    // ODRF entry is returned on the httpResponse header under "Location". this is expected behaviour
                    // this checks if "Location" is present in the header and if it has a value then
                    // it assumes that the draft was created.
                    // assumed instead of checking /Drafts because a GET call may be costly
                    var location = httpException.Call.Response.Headers.First(x => x.Name == "Location");
                    int? draftId = _getDraftEntryFromURI(location.Value);
                    return draftId == null ? throw new InvalidOperationException(ITR_NOT_POSTED_MSG, ex) : (int)draftId;
                }
                catch (Exception ex2)
                {
                    throw new InvalidOperationException(ITR_NOT_POSTED_MSG, ex2);
                }
            }
            else
            {
                throw;
            }
        }

        throw new InvalidOperationException("Something went wrong");
    }

    private int? _getDraftEntryFromURI(string uri)
    {
        string last = uri.Split("/").Last();
        string prefix = "Drafts(";
        string suffix = ")";
        if (!(last.StartsWith(prefix) && last.EndsWith(suffix))) return null;
        string mid = last.Substring(prefix.Length, last.Length - prefix.Length - suffix.Length);
        if (int.TryParse(mid, out int value))
            return value;
        return null;
    }
}